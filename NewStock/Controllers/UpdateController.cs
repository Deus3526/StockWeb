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
}
