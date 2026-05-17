using Microsoft.AspNetCore.Mvc;
using NewStock.Models;
using NewStock.Services;
using System.Text.Json.Serialization;

namespace NewStock.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class StrategyController(StrategyService strategyService) : ControllerBase
{
    /// <summary>
    /// 15日盤整後突破（對應 SP <c>dbo.[Strategy突破15日盤整]</c>，邏輯同 StockWeb Strategy25）。
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(StrategyBreakoutAfter15DaysResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StrategyBreakoutAfter15DaysResponse>> BreakoutAfter15Days(
        [FromQuery] DateOnly date,
        CancellationToken cancellationToken)
    {
        var rows = await strategyService.GetBreakoutAfter15DaysAsync(date, cancellationToken).ConfigureAwait(false);
        var summary = rows.Select(static r => new StrategyBreakoutAfter15DaysStockSummary(r.StockId, r.StockName)).ToArray();
        return Ok(new StrategyBreakoutAfter15DaysResponse(rows.Count, summary, rows));
    }
}

/// <summary>
/// 與 StockWeb <c>Strategy25</c> API 相同：<c>Result</c> 為簡表（StockId、StockName），<c>DetailResult</c> 為完整欄位。
/// </summary>
public sealed record StrategyBreakoutAfter15DaysStockSummary(short StockId, string StockName);

public sealed record StrategyBreakoutAfter15DaysResponse(
    [property: JsonPropertyOrder(0)] int Count,
    [property: JsonPropertyOrder(1)] IReadOnlyList<StrategyBreakoutAfter15DaysStockSummary> Result,
    [property: JsonPropertyOrder(2)] IReadOnlyList<StrategyBreakoutAfter15DaysRow> DetailResult);
