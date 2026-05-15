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
}
