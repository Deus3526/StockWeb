using Microsoft.EntityFrameworkCore;
using NewStock.EFModels;
using NewStock.Exceptions;
using NewStock.Extensions;
using NewStock.Finmind;
using NewStock.Models.Enum;
using System.Globalization;

namespace NewStock.Services;

public class UpdateService
{
    /// <summary>
    /// 與 StockWeb <c>GetDateMaxOrMinFromStockDayInfoAsync</c> 相同：資料庫尚無「盤後」<c>StockDayInfo</c> 時，以此日作為起算前的基準，下一個交易日為嚴格晚於此日之首個營業日。
    /// </summary>
    private static readonly DateOnly FallbackLatestDateWhenNoStockDayInfo = new(2024, 1, 2);

    /// <summary>
    /// 同時呼叫 FinMind TaiwanStockKBar 之上限（過高易觸發對方限流／連線耗盡）。
    /// </summary>
    private const int MinuteKFinMindConcurrency = 16;

    /// <summary>
    /// 每批寫入 <see cref="分K資料表"/> 之列數，以避免單次 SaveChangesAsync 附加過多追蹤實體。
    /// </summary>
    private const int MinuteKEfInsertBatchSize = 2500;

    /// <summary>
    /// 週／月 K 歷史補齊僅涵蓋此日期<strong>之前</strong>之 <see cref="TaiwanTradingDay"/>；
    /// 此日（含）起之資料由日線等整批流程處理，不由本 backfill 處理。
    /// </summary>
    private static readonly DateOnly ExclusiveUpperTradingDayForPeriodKBackfill = new(2024, 1, 2);

    private readonly FinmindApiClient _finmindApiClient;
    private readonly NewStockContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<UpdateService> _logger;

    public UpdateService(
        FinmindApiClient finmindApiClient,
        NewStockContext db,
        IHttpContextAccessor httpContextAccessor,
        ILogger<UpdateService> logger)
    {
        _finmindApiClient = finmindApiClient;
        _db = db;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 自 FinMind TaiwanStockInfo 更新至 StockInfo。
    /// <para>
    /// 字串欄位不加工、不補預設值；上游若缺值或無效，由 EF／資料庫約束反映錯誤。
    /// </para>
    /// </summary>
    public async Task<UpdateStockInfoResult> UpdateTaiwanStockInfoAsync()
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var rows = await _finmindApiClient.GetTaiwanStockInfoAsync(cancellationToken);
        var stockCount = rows.Count;

        var existing = await _db.StockInfos.ToDictionaryAsync(e => e.StockId, cancellationToken);

        var inserted = 0;
        var updated = 0;
        var unchanged = 0;

        foreach (var dto in rows)
        {
            if (existing.TryGetValue(dto.StockIdShort, out var entity))
            {
                if (entity.StockName == dto.StockName
                    && entity.IndustryCategory == dto.IndustryCategory
                    && entity.MarketType == dto.MarketTypeEnum)
                {
                    unchanged++;
                    continue;
                }

                entity.StockName = dto.StockName!;
                entity.IndustryCategory = dto.IndustryCategory!;
                entity.MarketType = dto.MarketTypeEnum;
                updated++;
            }
            else
            {
                _db.StockInfos.Add(new StockInfo
                {
                    StockId = dto.StockIdShort,
                    StockName = dto.StockName!,
                    IndustryCategory = dto.IndustryCategory!,
                    MarketType = dto.MarketTypeEnum,
                });
                inserted++;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation($"TaiwanStockInfo 更新完成：StockCount={stockCount}, Inserted={inserted}, Updated={updated}, Unchanged={unchanged}");

        return new UpdateStockInfoResult(stockCount, inserted, updated, unchanged);
    }

    /// <summary>
    /// 依 FinMind <c>TaiwanStockPrice</c>（單一交易日），更新資料庫 <see cref="StockDayInfo"/>。
    /// 先刪除庫中所有 <see cref="StockDayInfoDataTypeEnum.即時"/> 列，再以 <see cref="StockDayInfo"/>「盤後」之最大 <see cref="StockDayInfo.Date"/>（若尚無任何盤後列則比照 StockWeb 使用 2021-01-04）對照 <see cref="TaiwanTradingDay"/>，找嚴格晚於該日之最近一個交易日（必要時先呼叫 <see cref="UpdateTaiwanTradingDaysAsync"/>）。
    /// </summary>
    /// <remarks>
    /// 僅寫入 <see cref="StockInfo"/> 已存在之 StockId（外鍵）；若 FinMind 回傳之列不在 StockInfo，則略過並於回應附訊息（如下市、新股未同步等基本資料）。
    /// 刪除即時後，表中僅會留下非即時列（正常為盤後）；決定下一個交易日時僅統計「盤後」之最大日期，若無盤後列則見 <see cref="FallbackLatestDateWhenNoStockDayInfo"/>。
    /// 在正常流程下，選出之交易日嚴格晚於上開最大日；接著寫入之盤後列為新一筆。
    /// FinMind <c>Trading_Volume</c> 為股數，<see cref="StockDayInfo.成交量"/> 存張數（除以 1000），欄位為 <see cref="long"/>；
    /// <see cref="StockDayInfo.成交筆數"/> 對應 FinMind <c>Trading_turnover</c>。
    /// 平盤價／漲幅見 <see cref="TaiwanStockPriceResponse.平盤價"/>／<see cref="TaiwanStockPriceResponse.漲幅"/> 與 <see cref="TaiwanStockPriceResponse.Spread"/>。
    /// 完成日線寫入後，比對「先前最後一筆盤後日」（庫內最大盤後日；若尚無盤後列則見 <see cref="FallbackLatestDateWhenNoStockDayInfo"/>）與本次 <c>tradingDay</c>：若<strong>不同曆週</strong>則以 <see cref="DateOnlyExtensions.GetMondayOfCalendarWeek"/>（<c>tradingDay</c> 所屬曆週之週一，該日休市亦可）呼叫 <see cref="UpdateTaiwanStockWeekKAsync"/>；若<strong>不同曆月</strong>則以 <see cref="DateOnlyExtensions.GetFirstDayOfCalendarMonth"/>（<c>tradingDay</c> 所屬曆月 1 日）呼叫 <see cref="UpdateTaiwanStockMonthKAsync"/>。
    /// （另可於此串接 <see cref="UpdateTaiwanStockKBarAsync"/> 置換當日分 K；為避免 FinMind API 過於頻繁，目前程式已註解關閉，需要時可自行還原。）
    /// </remarks>
    public async Task<UpdateStockDayInfoResult> UpdateStockDayInfoAsync()
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        await _db.StockDayInfos
            .Where(x => x.DataType == StockDayInfoDataTypeEnum.即時)
            .ExecuteDeleteAsync(cancellationToken);

        var latestDateInStockDayInfo = await _db.StockDayInfos
            .Where(x => x.DataType == StockDayInfoDataTypeEnum.盤後)
            .OrderByDescending(x => x.Date)
            .Select(x => (DateOnly?)x.Date)
            .FirstOrDefaultAsync(cancellationToken) ?? FallbackLatestDateWhenNoStockDayInfo;

        var tradingDay = await GetNextStockDayInfoTradingDayAsync(latestDateInStockDayInfo, cancellationToken);

        var allowedStockIds =
            await _db.StockInfos.AsNoTracking().Select(s => s.StockId).ToHashSetAsync(cancellationToken);

        var rows = await _finmindApiClient.GetTaiwanStockPriceForTradingDayAsync(tradingDay, cancellationToken);

        var inserted = 0;
        var skippedNotInStockInfo = 0;
        var skippedStockIds = new HashSet<short>();

        foreach (var dto in rows)
        {
            if (!allowedStockIds.Contains(dto.StockIdShort))
            {
                skippedNotInStockInfo++;
                skippedStockIds.Add(dto.StockIdShort);
                continue;
            }

            _db.StockDayInfos.Add(new StockDayInfo
            {
                Date = tradingDay,
                StockId = dto.StockIdShort,
                DataType = StockDayInfoDataTypeEnum.盤後,
                開盤價 = dto.Open,
                最高價 = dto.High,
                最低價 = dto.Low,
                收盤價 = dto.Close,
                成交量 = dto.TradingVolume / 1000L,
                成交筆數 = dto.TradingTurnover,
                漲幅 = dto.漲幅,
                平盤價 = dto.平盤價,
            });
            inserted++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        UpdateStockPeriodKResult? weekKResult = null;
        UpdateStockPeriodKResult? monthKResult = null;

        if (latestDateInStockDayInfo.AreInDifferentCalendarWeeks(tradingDay))
        {
            var weekMonday = tradingDay.GetMondayOfCalendarWeek();
            _logger.LogInformation(
                "StockDayInfo 連動：盤後日與本輪交易日不同曆週，更新週 K… Last盤後={Last:yyyy-MM-dd}, TradingDay={Curr:yyyy-MM-dd}, WeekMonday={WeekMon:yyyy-MM-dd}",
                latestDateInStockDayInfo,
                tradingDay,
                weekMonday);
            weekKResult = await UpdateTaiwanStockWeekKAsync(weekMonday).ConfigureAwait(false);
        }

        if (latestDateInStockDayInfo.AreInDifferentCalendarMonths(tradingDay))
        {
            var monthFirst = tradingDay.GetFirstDayOfCalendarMonth();
            _logger.LogInformation(
                "StockDayInfo 連動：盤後日與本輪交易日不同曆月，更新月 K… MonthFirst={MonthFirst:yyyy-MM-dd}, TradingDay={Curr:yyyy-MM-dd}",
                monthFirst,
                tradingDay);
            monthKResult = await UpdateTaiwanStockMonthKAsync(monthFirst).ConfigureAwait(false);
        }

        // 分 K：易觸發 FinMind 限流，暫時關閉；請改呼叫 api/Update/UpdateTaiwanStockKBar。
        /*
        var minuteKResult = await UpdateTaiwanStockKBarAsync(tradingDay).ConfigureAwait(false);
        if (minuteKResult.ApiFailedStocks > 0)
        {
            throw new HttpStatusCodeException(
                StatusCodes.Status400BadRequest,
                minuteKResult.Message ?? $"分 K FinMind API 有 {minuteKResult.ApiFailedStocks} 檔失敗（TradingDay={tradingDay:yyyy-MM-dd}）。");
        }
        */

        string? message = null;
        if (skippedNotInStockInfo > 0)
        {
            message =
                $"已略過 {skippedNotInStockInfo} 筆 TaiwanStockPrice 列（{skippedStockIds.Count} 個不重複代號未見於 StockInfo；可能為已下市、或新股／尚未同步，可視需要執行 UpdateStockInfo）。";
            if (skippedStockIds.Count <= 30)
                message += " 代號：" + string.Join(",", skippedStockIds.Order());
        }

        _logger.LogInformation($"TaiwanStockPrice 更新：TradingDay={tradingDay:yyyy-MM-dd}, ApiRows={rows.Count}, Inserted={inserted}, SkippedNotInStockInfo={skippedNotInStockInfo}");

        return new UpdateStockDayInfoResult(
            TradingDay: tradingDay,
            ApiRowCount: rows.Count,
            Inserted: inserted,
            SkippedNotInStockInfo: skippedNotInStockInfo,
            Message: message,
            WeekK: weekKResult,
            MonthK: monthKResult,
            MinuteK: null); // 分 K 已註解，見上；還原串接後改回 minuteKResult
    }

    /// <summary>
    /// 已知庫中「盤後」資料之最大交易日為 <paramref name="latestDateInStockDayInfo"/>（若資料庫原無盤後列則已由呼叫端套用與 StockWeb 相同之 <see cref="FallbackLatestDateWhenNoStockDayInfo"/>），自 <see cref="TaiwanTradingDay"/> 取得下一個交易日；必要時先同步交易日曆。
    /// </summary>
    private async Task<DateOnly> GetNextStockDayInfoTradingDayAsync(DateOnly latestDateInStockDayInfo, CancellationToken cancellationToken)
    {
        var next = await _db.TaiwanTradingDays.AsNoTracking()
            .Where(t => t.Date > latestDateInStockDayInfo)
            .OrderBy(t => t.Date)
            .Select(t => (DateOnly?)t.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (next.HasValue)
            return next.Value;

        await UpdateTaiwanTradingDaysAsync(latestDateInStockDayInfo);

        next = await _db.TaiwanTradingDays.AsNoTracking()
            .Where(t => t.Date > latestDateInStockDayInfo)
            .OrderBy(t => t.Date)
            .Select(t => (DateOnly?)t.Date)
            .FirstOrDefaultAsync(cancellationToken);

        if (!next.HasValue)
        {
            throw new HttpStatusCodeException(
                StatusCodes.Status400BadRequest,
                $"已自 {latestDateInStockDayInfo:yyyy-MM-dd} 同步 TaiwanTradingDay 至今天，仍無下一個可更新的交易日。");
        }

        return next.Value;
    }

    /// <summary>
    /// 自 FinMind TaiwanStockTradingDate 擷取 <paramref name="dateFrom"/> 至今天（本機日期）的交易日寫入 TaiwanTradingDay；已存在之日期略過。
    /// </summary>
    public async Task<UpdateTaiwanTradingDaysResult> UpdateTaiwanTradingDaysAsync(DateOnly dateFrom)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (dateFrom == default)
            throw new HttpStatusCodeException(StatusCodes.Status400BadRequest, "請提供 dateFrom（yyyy-MM-dd），不可為 default。");

        if (dateFrom > today)
            throw new HttpStatusCodeException(StatusCodes.Status400BadRequest, "dateFrom 不可大於今天（本機日期）。");

        var tradingDays =
            await _finmindApiClient.GetTaiwanStockTradingDatesAsync(dateFrom, today, cancellationToken);

        var existingInRange = await _db.TaiwanTradingDays
            .Where(t => t.Date >= dateFrom && t.Date <= today)
            .Select(t => t.Date)
            .ToHashSetAsync(cancellationToken);

        var inserted = 0;
        foreach (var d in tradingDays)
        {
            if (existingInRange.Contains(d))
                continue;

            _db.TaiwanTradingDays.Add(new TaiwanTradingDay { Date = d });
            existingInRange.Add(d);
            inserted++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        var skippedExisting = tradingDays.Count - inserted;

        _logger.LogInformation($"TaiwanStockTradingDate 同步：DateFrom={dateFrom:yyyy-MM-dd}, Today={today:yyyy-MM-dd}, TradingDays={tradingDays.Count}, Inserted={inserted}, SkippedExisting={skippedExisting}");

        return new UpdateTaiwanTradingDaysResult(
            DateFrom: dateFrom,
            DateTo: today,
            TradingDayCount: tradingDays.Count,
            Inserted: inserted,
            SkippedExisting: skippedExisting);
    }

    /// <summary>
    /// FinMind <c>TaiwanStockWeekPrice</c>（<paramref name="date"/> 須為該根週 K 之週一）置換 <see cref="周月K資料表"/>：先刪除庫中同年月日且 <see cref="StockKBarTimeTypeEnum.Week"/> 之列，再自 API 全量插入（僅 <see cref="StockInfo"/> 已存在之代號）；寫入之 <see cref="周月K資料表.Date"/> 為 API 列之 <c>date</c>。
    /// </summary>
    /// <remarks>刪除發生在呼叫 FinMind 之前；若之後 API 失敗，該週區間在庫中會暫為空，請留意重跑。</remarks>
    public async Task<UpdateStockPeriodKResult> UpdateTaiwanStockWeekKAsync(DateOnly date)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        if (date == default)
            throw new HttpStatusCodeException(StatusCodes.Status400BadRequest, "請提供 date（yyyy-MM-dd），不可為 default。");

        if (date.DayOfWeek != DayOfWeek.Monday)
        {
            throw new HttpStatusCodeException(
                StatusCodes.Status400BadRequest,
                $"週 K 的 date 須為該週星期一，目前為「{date.DayOfWeek:G}」({date:yyyy-MM-dd})。");
        }

        const StockKBarTimeTypeEnum timeType = StockKBarTimeTypeEnum.Week;

        await _db.周月K資料表s
            .Where(e => e.Date == date && e.TimeType == timeType)
            .ExecuteDeleteAsync(cancellationToken);

        var rows = await _finmindApiClient.GetTaiwanStockWeekPriceAsync(date, cancellationToken);

        var allowedStockIds =
            await _db.StockInfos.AsNoTracking().Select(s => s.StockId).ToHashSetAsync(cancellationToken);

        var inserted = 0;
        var skippedNotInStockInfo = 0;
        var skippedStockIds = new HashSet<short>();

        foreach (var dto in rows)
        {
            var stockId = dto.StockIdShort;
            if (!allowedStockIds.Contains(stockId))
            {
                skippedNotInStockInfo++;
                skippedStockIds.Add(stockId);
                continue;
            }

            _db.周月K資料表s.Add(new 周月K資料表
            {
                StockId = stockId,
                Date = dto.Date,
                TimeType = timeType,
                開盤價 = dto.Open,
                最高價 = dto.High,
                最低價 = dto.Low,
                收盤價 = dto.Close,
                平盤價 = dto.平盤價,
                漲幅 = dto.漲幅,
                交易筆數 = dto.TradingTurnover,
                DataType = StockDayInfoDataTypeEnum.盤後,
            });
            inserted++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        string? message = null;
        if (skippedNotInStockInfo > 0)
        {
            message =
                $"已略過 {skippedNotInStockInfo} 筆 TaiwanStockWeekPrice 列（{skippedStockIds.Count} 個不重複代號未見於 StockInfo；可視需要執行 UpdateStockInfo）。";
            if (skippedStockIds.Count <= 30)
                message += " 代號：" + string.Join(",", skippedStockIds.Order());
        }

        _logger.LogInformation($"FinMind TaiwanStockWeekPrice：PeriodStart={date:yyyy-MM-dd}, ApiRows={rows.Count}, Inserted={inserted}, Updated=0, SkippedNotInStockInfo={skippedNotInStockInfo}");

        return new UpdateStockPeriodKResult(
            PeriodStart: date,
            TimeType: timeType,
            ApiRowCount: rows.Count,
            Inserted: inserted,
            Updated: 0,
            SkippedNotInStockInfo: skippedNotInStockInfo,
            Message: message);
    }

    /// <summary>
    /// FinMind <c>TaiwanStockMonthPrice</c>（<paramref name="date"/> 須為每月 1 號）置換 <see cref="周月K資料表"/>：先刪除同年月日且 <see cref="StockKBarTimeTypeEnum.Month"/> 之列，再自 API 全量插入；欄位對應同 <see cref="UpdateTaiwanStockWeekKAsync"/>（寫入之 <see cref="周月K資料表.Date"/> 為 API 列之 <c>date</c>）。
    /// </summary>
    /// <remarks>刪除發生在呼叫 FinMind 之前；若之後 API 失敗，該月區間在庫中會暫為空，請留意重跑。</remarks>
    public async Task<UpdateStockPeriodKResult> UpdateTaiwanStockMonthKAsync(DateOnly date)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        if (date == default)
            throw new HttpStatusCodeException(StatusCodes.Status400BadRequest, "請提供 date（yyyy-MM-dd），不可為 default。");

        if (date.Day != 1)
        {
            throw new HttpStatusCodeException(
                StatusCodes.Status400BadRequest,
                $"月 K 的 date 須為每月 1 號，目前為 {date:yyyy-MM-dd}。");
        }

        const StockKBarTimeTypeEnum timeType = StockKBarTimeTypeEnum.Month;

        await _db.周月K資料表s
            .Where(e => e.Date == date && e.TimeType == timeType)
            .ExecuteDeleteAsync(cancellationToken);

        var rows = await _finmindApiClient.GetTaiwanStockMonthPriceAsync(date, cancellationToken);

        var allowedStockIds =
            await _db.StockInfos.AsNoTracking().Select(s => s.StockId).ToHashSetAsync(cancellationToken);

        var inserted = 0;
        var skippedNotInStockInfo = 0;
        var skippedStockIds = new HashSet<short>();

        foreach (var dto in rows)
        {
            var stockId = dto.StockIdShort;
            if (!allowedStockIds.Contains(stockId))
            {
                skippedNotInStockInfo++;
                skippedStockIds.Add(stockId);
                continue;
            }

            _db.周月K資料表s.Add(new 周月K資料表
            {
                StockId = stockId,
                Date = dto.Date,
                TimeType = timeType,
                開盤價 = dto.Open,
                最高價 = dto.High,
                最低價 = dto.Low,
                收盤價 = dto.Close,
                平盤價 = dto.平盤價,
                漲幅 = dto.漲幅,
                交易筆數 = dto.TradingTurnover,
                DataType = StockDayInfoDataTypeEnum.盤後,
            });
            inserted++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        string? message = null;
        if (skippedNotInStockInfo > 0)
        {
            message =
                $"已略過 {skippedNotInStockInfo} 筆 TaiwanStockMonthPrice 列（{skippedStockIds.Count} 個不重複代號未見於 StockInfo；可視需要執行 UpdateStockInfo）。";
            if (skippedStockIds.Count <= 30)
                message += " 代號：" + string.Join(",", skippedStockIds.Order());
        }

        _logger.LogInformation($"FinMind TaiwanStockMonthPrice：PeriodStart={date:yyyy-MM-dd}, ApiRows={rows.Count}, Inserted={inserted}, Updated=0, SkippedNotInStockInfo={skippedNotInStockInfo}");

        return new UpdateStockPeriodKResult(
            PeriodStart: date,
            TimeType: timeType,
            ApiRowCount: rows.Count,
            Inserted: inserted,
            Updated: 0,
            SkippedNotInStockInfo: skippedNotInStockInfo,
            Message: message);
    }

    /// <summary>
    /// 單次僅處理<strong>一個</strong>交易日：<see cref="NewStock.EFModels.TaiwanTradingDay"/> 篩選 <c>Date &lt; 2024-01-02</c>；
    /// <paramref name="lastTradingDay"/> 為 null 時取區間內<strong>最早</strong>交易日，否則取<strong>嚴格晚於</strong>該日之下一個交易日。
    /// 比對上一輪 <paramref name="lastTradingDay"/> 與本輪「下一個」交易日：若<strong>不同曆週</strong>則以該日所屬曆週一（休市亦可）補週 K；若<strong>不同曆月</strong>則以該月 1 日補月 K。首次呼叫 <paramref name="lastTradingDay"/> 為 null 時兩者皆補。回傳之 <see cref="BackfillPeriodKResult.TradingDayProcessed"/> 供下次帶入 <paramref name="lastTradingDay"/>。
    /// </summary>
    public async Task<BackfillPeriodKResult> BackfillPeriodKFromTradingDaysAsync(DateOnly? lastTradingDay)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var upper = ExclusiveUpperTradingDayForPeriodKBackfill;

        var inRange = _db.TaiwanTradingDays.AsNoTracking().Where(t => t.Date < upper);

        var current = lastTradingDay is null
            ? await inRange.OrderBy(t => t.Date).Select(t => (DateOnly?)t.Date).FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false)
            : await inRange.Where(t => t.Date > lastTradingDay.Value).OrderBy(t => t.Date).Select(t => (DateOnly?)t.Date)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (current is null)
        {
            var msg = lastTradingDay is null
                ? "在可補齊區間內沒有任何 TaiwanTradingDay。"
                : "已無更晚的交易日（上一輪交易日之後無資料，或已超出區間）。";

            _logger.LogInformation("BackfillPeriodK 單步：無本輪交易日。{Msg}", msg);

            return new BackfillPeriodKResult(
                TradingDayProcessed: null,
                TradingDayExclusiveUpper: upper,
                HasMoreAfterThisDay: false,
                WeekKUpdated: false,
                MonthKUpdated: false,
                Message: msg);
        }

        var day = current.Value;

        var needWeek = lastTradingDay is null
            || lastTradingDay.Value.AreInDifferentCalendarWeeks(day);
        var needMonth = lastTradingDay is null
            || lastTradingDay.Value.AreInDifferentCalendarMonths(day);

        var weekKUpdated = false;
        if (needWeek)
        {
            var weekMonday = day.GetMondayOfCalendarWeek();
            _logger.LogInformation(
                "BackfillPeriodK 單步：上一輪與本輪不同曆週，補週 K WeekMonday={Wm:yyyy-MM-dd}, TradingDay={Td:yyyy-MM-dd}",
                weekMonday,
                day);
            await UpdateTaiwanStockWeekKAsync(weekMonday).ConfigureAwait(false);
            _db.ChangeTracker.Clear();
            weekKUpdated = true;
        }

        var monthKUpdated = false;
        if (needMonth)
        {
            var monthFirst = day.GetFirstDayOfCalendarMonth();
            _logger.LogInformation(
                "BackfillPeriodK 單步：上一輪與本輪不同曆月，補月 K MonthFirst={Mf:yyyy-MM-dd}, TradingDay={Td:yyyy-MM-dd}",
                monthFirst,
                day);
            await UpdateTaiwanStockMonthKAsync(monthFirst).ConfigureAwait(false);
            _db.ChangeTracker.Clear();
            monthKUpdated = true;
        }

        var hasMore = await inRange.AnyAsync(t => t.Date > day, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "BackfillPeriodK 單步完成：TradingDay={Td:yyyy-MM-dd}，尚有待處理較晚交易日={More}",
            day,
            hasMore);

        return new BackfillPeriodKResult(
            TradingDayProcessed: day,
            TradingDayExclusiveUpper: upper,
            HasMoreAfterThisDay: hasMore,
            WeekKUpdated: weekKUpdated,
            MonthKUpdated: monthKUpdated,
            Message: null);
    }

    /// <summary>
    /// FinMind <c>TaiwanStockKBar</c>：置換 <paramref name="date"/> 當日之全部 <see cref="分K資料表"/>（先刪後插）。
    /// 代號清單取自 <see cref="StockInfo"/>；每檔以非同步工作呼叫 FinMind，並以 <see cref="MinuteKFinMindConcurrency"/> 限制並行，最後 <c>Task.WhenAll</c> 匯總後以 <see cref="MinuteKEfInsertBatchSize"/> 分批寫入。
    /// </summary>
    /// <remarks>
    /// 單次 SaveChangesAsync 若附加數萬筆變更，變更追蹤成本與產生的 SQL／逾時風險皆可能過高；故採分批儲存並於每批後呼叫 <c>DbContext.ChangeTracker.Clear()</c>。
    /// 若某檔 API 例外，會記錄警告並計入 <see cref="UpdateAllStocksMinuteKResult.ApiFailedStocks"/>，其餘檔照常寫入。
    /// HTTP 介面：<see cref="Controllers.UpdateController.UpdateTaiwanStockKBar"/> 在 <c>ApiFailedStocks &gt; 0</c> 時回傳 400。
    /// </remarks>
    public async Task<UpdateAllStocksMinuteKResult> UpdateTaiwanStockKBarAsync(DateOnly date)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        if (date == default)
            throw new HttpStatusCodeException(StatusCodes.Status400BadRequest, "請提供 date（yyyy-MM-dd），不可為 default。");

        await _db.分K資料表s.Where(e => e.Date == date).ExecuteDeleteAsync(cancellationToken);

        var stockIds = await _db.StockInfos.AsNoTracking().Select(s => s.StockId).ToListAsync(cancellationToken);

        var semaphore = new SemaphoreSlim(MinuteKFinMindConcurrency);

        async Task<MinuteKFetchOutcome> FetchAndMapAsync(short stockId)
        {
            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var rows = await _finmindApiClient.GetTaiwanStockKBarAsync(date, stockId, cancellationToken).ConfigureAwait(false);
                var entities = new List<分K資料表>(rows.Count);
                foreach (var dto in rows)
                {
                    entities.Add(new 分K資料表
                    {
                        StockId = stockId,
                        Date = dto.Date,
                        分鐘 = TimeOnly.Parse(dto.Minute!, CultureInfo.InvariantCulture),
                        開盤價 = dto.Open,
                        最高價 = dto.High,
                        最低價 = dto.Low,
                        收盤價 = dto.Close,
                        成交量 = dto.Volume,
                        DataType = StockDayInfoDataTypeEnum.盤後,
                    });
                }

                _logger.LogInformation(
                    "FinMind TaiwanStockKBar 單檔完成：StockId={StockId}, Date={Date:yyyy-MM-dd}, Rows={RowCount}",
                    stockId,
                    date,
                    entities.Count);

                return new MinuteKFetchOutcome(stockId, entities, ApiOk: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "FinMind TaiwanStockKBar 失敗：StockId={StockId}, Date={Date:yyyy-MM-dd}",
                    stockId,
                    date);
                return new MinuteKFetchOutcome(stockId, [], ApiOk: false);
            }
            finally
            {
                semaphore.Release();
            }
        }

        var outcomes = await Task.WhenAll(stockIds.Select(id => FetchAndMapAsync(id))).ConfigureAwait(false);

        var apiSucceededStocks = outcomes.Count(o => o.ApiOk);
        var apiFailedStocks = outcomes.Length - apiSucceededStocks;
        // 各檔一支 List；SelectMany 僅展開為序列（不先 ToList 複製成單一大 List），再以 Chunk 分批寫入。
        var apiRowCountSum = outcomes.Sum(o => o.Entities.Count);

        var inserted = 0;
        foreach (var chunk in outcomes.SelectMany(o => o.Entities).Chunk(MinuteKEfInsertBatchSize))
        {
            await _db.分K資料表s.AddRangeAsync(chunk, cancellationToken).ConfigureAwait(false);
            inserted += chunk.Length;
            await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _db.ChangeTracker.Clear();
        }

        string? message = null;
        if (apiFailedStocks > 0)
        {
            message = $"API 失敗檔數：{apiFailedStocks}。";
            var failedIds = outcomes.Where(o => !o.ApiOk).Select(o => o.StockId).OrderBy(id => id).ToList();
            if (failedIds.Count > 0 && failedIds.Count <= 40)
                message += $" 失敗代號：" + string.Join(",", failedIds);
            else if (failedIds.Count > 40)
                message += $" 失敗代號（前40）：" + string.Join(",", failedIds.Take(40));
        }

        _logger.LogInformation(
            $"FinMind TaiwanStockKBar：Date={date:yyyy-MM-dd}, StockTotal={stockIds.Count}, Ok={apiSucceededStocks}, Fail={apiFailedStocks}, ApiRowsSum={apiRowCountSum}, Inserted={inserted}");

        return new UpdateAllStocksMinuteKResult(
            Date: date,
            StockTotal: stockIds.Count,
            ApiSucceededStocks: apiSucceededStocks,
            ApiFailedStocks: apiFailedStocks,
            ApiRowCountSum: apiRowCountSum,
            Inserted: inserted,
            Updated: 0,
            Message: message);
    }

    private sealed record MinuteKFetchOutcome(
        short StockId,
        List<分K資料表> Entities,
        bool ApiOk);
}

public sealed record UpdateStockInfoResult(
    int StockCount,
    int Inserted,
    int Updated,
    int Unchanged);

/// <summary>
/// <see cref="UpdateService.UpdateStockDayInfoAsync"/> 之結果摘要；
/// <see cref="SkippedNotInStockInfo"/> 為 StockInfo 無對應而略過之筆數，<see cref="Message"/> 為情境說明（無略過時為 null）。
/// <see cref="WeekK"/>／<see cref="MonthK"/> 為本次連動之週／月 K 置換結果；未觸發時為 null。
/// <see cref="MinuteK"/> 若有串接當日分 K 更新則填入；目前已暫停內建分 K，預設為 null。
/// </summary>
public sealed record UpdateStockDayInfoResult(
    DateOnly TradingDay,
    int ApiRowCount,
    int Inserted,
    int SkippedNotInStockInfo,
    string? Message,
    UpdateStockPeriodKResult? WeekK = null,
    UpdateStockPeriodKResult? MonthK = null,
    UpdateAllStocksMinuteKResult? MinuteK = null);

public sealed record UpdateTaiwanTradingDaysResult(
    DateOnly DateFrom,
    DateOnly DateTo,
    int TradingDayCount,
    int Inserted,
    int SkippedExisting);

/// <summary>
/// <see cref="UpdateService.UpdateTaiwanStockWeekKAsync"/>／<see cref="UpdateService.UpdateTaiwanStockMonthKAsync"/> 之摘要；
/// <see cref="Updated"/> 於週／月 K 為置換寫入（先刪後插），固定為 0。
/// </summary>
public sealed record UpdateStockPeriodKResult(
    DateOnly PeriodStart,
    StockKBarTimeTypeEnum TimeType,
    int ApiRowCount,
    int Inserted,
    int Updated,
    int SkippedNotInStockInfo,
    string? Message);

/// <summary>
/// <see cref="UpdateService.BackfillPeriodKFromTradingDaysAsync"/> 單步結果。
/// <see cref="TradingDayProcessed"/> 為本輪處理之交易日；下次請帶入查詢 <c>lastTradingDay</c> 以續跑。
/// <see cref="TradingDayExclusiveUpper"/> 為內建上界（<c>TaiwanTradingDay.Date &lt; 該日</c> 才納入）。
/// </summary>
public sealed record BackfillPeriodKResult(
    DateOnly? TradingDayProcessed,
    DateOnly TradingDayExclusiveUpper,
    bool HasMoreAfterThisDay,
    bool WeekKUpdated,
    bool MonthKUpdated,
    string? Message);

/// <summary><see cref="UpdateService.UpdateTaiwanStockKBarAsync"/> 之摘要（全 StockInfo）。</summary>
public sealed record UpdateAllStocksMinuteKResult(
    DateOnly Date,
    int StockTotal,
    int ApiSucceededStocks,
    int ApiFailedStocks,
    int ApiRowCountSum,
    int Inserted,
    int Updated,
    string? Message);
