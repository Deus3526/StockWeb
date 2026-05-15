using Microsoft.EntityFrameworkCore;
using NewStock.EFModels;
using NewStock.Exceptions;
using NewStock.Finmind;
using NewStock.Models.Enum;

namespace NewStock.Services;

public class UpdateService
{
    /// <summary>
    /// 與 StockWeb <c>GetDateMaxOrMinFromStockDayInfoAsync</c> 相同：資料庫尚無「盤後」<c>StockDayInfo</c> 時，以此日作為起算前的基準，下一個交易日為嚴格晚於此日之首個營業日。
    /// </summary>
    private static readonly DateOnly FallbackLatestDateWhenNoStockDayInfo = new(2021, 1, 4);

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
    /// FinMind <c>Trading_Volume</c> 為股數，<see cref="StockDayInfo.成交量"/> 存張數（除以 1000），欄位為 <see cref="long"/>。
    /// 平盤價／漲幅見 <see cref="TaiwanStockPriceResponse.平盤價"/>／<see cref="TaiwanStockPriceResponse.漲幅"/> 與 <see cref="TaiwanStockPriceResponse.Spread"/>。
    /// </remarks>
    public async Task<UpdateStockDayInfoResult> UpdateStockDayInfoAsync()
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;

        await _db.StockDayInfos
            .Where(x => x.DataType == StockDayInfoDataTypeEnum.即時)
            .ExecuteDeleteAsync(cancellationToken);

        var latestDateNullable = await _db.StockDayInfos
            .Where(x => x.DataType == StockDayInfoDataTypeEnum.盤後)
            .OrderByDescending(x => x.Date)
            .Select(x => (DateOnly?)x.Date)
            .FirstOrDefaultAsync(cancellationToken);

        var latestDateInStockDayInfo = latestDateNullable ?? FallbackLatestDateWhenNoStockDayInfo;

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
                漲幅 = dto.漲幅,
                平盤價 = dto.平盤價,
            });
            inserted++;
        }

        await _db.SaveChangesAsync(cancellationToken);

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
            Message: message);
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
}

public sealed record UpdateStockInfoResult(
    int StockCount,
    int Inserted,
    int Updated,
    int Unchanged);

/// <summary>
/// <see cref="UpdateService.UpdateStockDayInfoAsync"/> 之結果摘要；
/// <see cref="SkippedNotInStockInfo"/> 為 StockInfo 無對應而略過之筆數，<see cref="Message"/> 為情境說明（無略過時為 null）。
/// </summary>
public sealed record UpdateStockDayInfoResult(
    DateOnly TradingDay,
    int ApiRowCount,
    int Inserted,
    int SkippedNotInStockInfo,
    string? Message);

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
