using Microsoft.AspNetCore.Mvc;
using NewStock.Services;

namespace NewStock.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UpdateController : ControllerBase
{
    private readonly UpdateService _updateService;

    public UpdateController(UpdateService updateService)
    {
        _updateService = updateService;
    }

    /// <summary>
    /// 自 FinMind TaiwanStockInfo 更新股票基本資料至資料庫 StockInfo。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateStockInfoResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateStockInfoResult>> UpdateStockInfoAsync()
    {
        var result = await _updateService.UpdateTaiwanStockInfoAsync();
        return Ok(result);
    }

    /// <summary>
    /// 依 FinMind TaiwanStockPrice 更新 StockDayInfo：自動以目前「盤後」最新日推算下一個 TaiwanTradingDay，必要時先同步交易日曆。
    /// 成功寫入日線後，若「先前最後一筆盤後日」與本輪交易日<strong>不同曆週</strong>則以該週曆週一連動週 K；<strong>不同曆月</strong>則以當月 1 日連動月 K（FinMind 週 K 之 <c>date</c> 可為休市之週一）。分 K 另行呼叫 <c>UpdateTaiwanStockKBar</c>（本 API 內已暫停串接以免限流）。
    /// 若無下一交易日或周／月 K 參數錯誤，回應 <strong>400</strong>（訊息見 body）；成功時週／月結果於 <see cref="UpdateStockDayInfoResult.WeekK"/>／<see cref="UpdateStockDayInfoResult.MonthK"/>（未觸發為 null），<see cref="UpdateStockDayInfoResult.MinuteK"/> 目前為 null。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateStockDayInfoResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateStockDayInfoResult>> UpdateStockDayInfo()
    {
        var result = await _updateService.UpdateStockDayInfoAsync();
        return Ok(result);
    }

    /// <summary>
    /// 自 FinMind TaiwanStockTradingDate，將 <paramref name="dateFrom"/>（含）至今天（伺服器本機日期）的交易日寫入 TaiwanTradingDay。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateTaiwanTradingDaysResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateTaiwanTradingDaysResult>> UpdateTaiwanTradingDays(
        [FromQuery] DateOnly dateFrom)
    {
        var result = await _updateService.UpdateTaiwanTradingDaysAsync(dateFrom);
        return Ok(result);
    }

    /// <summary>
    /// FinMind TaiwanStockWeekPrice：<paramref name="date"/> 為該根 K 對應之週一（yyyy-MM-dd），寫入／更新資料庫週 K。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateStockPeriodKResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateStockPeriodKResult>> UpdateTaiwanStockWeekK([FromQuery] DateOnly date)
    {
        var result = await _updateService.UpdateTaiwanStockWeekKAsync(date);
        return Ok(result);
    }

    /// <summary>
    /// FinMind TaiwanStockMonthPrice：<paramref name="date"/> 為該根 K 對應之月初（每月 1 號，yyyy-MM-dd），寫入／更新資料庫月 K。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateStockPeriodKResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateStockPeriodKResult>> UpdateTaiwanStockMonthK([FromQuery] DateOnly date)
    {
        var result = await _updateService.UpdateTaiwanStockMonthKAsync(date);
        return Ok(result);
    }

    /// <summary>
    /// 週／月 K 歷史補齊（<strong>單步</strong>）：僅處理一個交易日。未帶 <paramref name="lastTradingDay"/> 時取最早一筆符合區間之交易日；有帶時取<strong>嚴格晚於</strong>該日之下一筆。比對上一輪與本輪交易日之曆週／曆月，跨週或跨月時分別以曆週一、月初補週／月 K。回傳之 <see cref="BackfillPeriodKResult.TradingDayProcessed"/> 下次作為 <paramref name="lastTradingDay"/>。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BackfillPeriodKResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BackfillPeriodKResult>> BackfillPeriodKFromTradingDays(
        [FromQuery] DateOnly? lastTradingDay = null)
    {
        var result = await _updateService.BackfillPeriodKFromTradingDaysAsync(lastTradingDay);
        return Ok(result);
    }

    /// <summary>
    /// FinMind TaiwanStockKBar：<paramref name="date"/> 為交易日（yyyy-MM-dd）；自 StockInfo 逐檔非同步抓取並置換該日全部分 K。若有任一檔 FinMind API 失敗則回傳 400（內容仍為彙總結果）。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateAllStocksMinuteKResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UpdateAllStocksMinuteKResult), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UpdateAllStocksMinuteKResult>> UpdateTaiwanStockKBar([FromQuery] DateOnly date)
    {
        var result = await _updateService.UpdateTaiwanStockKBarAsync(date);
        if (result.ApiFailedStocks > 0)
            return BadRequest(result);
        return Ok(result);
    }

    /// <summary>
    /// FinMind <c>taiwan_stock_tick_snapshot</c>（Bearer，見 <c>Finmind/ApiTest/即時資料.http</c>）：以<strong>全市場</strong>快照更新當日盤中即時列至 <see cref="EFModels.StockDayInfo"/>（<c>StockDayInfo.DataType</c>=即時）。
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UpdateTaiwanStockTickSnapshotResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<UpdateTaiwanStockTickSnapshotResult>> UpdateRealtimeStockQuotes()
    {
        var result = await _updateService.UpdateTaiwanStockTickSnapshotAsync();
        return Ok(result);
    }
}
