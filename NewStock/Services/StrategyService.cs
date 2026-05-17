using Microsoft.EntityFrameworkCore;
using NewStock.EFModels;
using NewStock.Models;

namespace NewStock.Services;

/// <summary>
/// 策略預存程序呼叫（對齊 StockWeb：<c>_db.Database.SqlQuery&lt;T&gt;($"exec … @date={date}")</c>）。
/// </summary>
public sealed class StrategyService(NewStockContext db)
{
    /// <summary>
    /// 執行 <c>[dbo].[Strategy突破15日盤整]</c>。
    /// </summary>
    public async Task<IReadOnlyList<StrategyBreakoutAfter15DaysRow>> GetBreakoutAfter15DaysAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.Database
            .SqlQuery<StrategyBreakoutAfter15DaysRow>($"exec [dbo].[Strategy突破15日盤整] @date={date}")
            .ToListAsync(cancellationToken);

        return rows;
    }
}