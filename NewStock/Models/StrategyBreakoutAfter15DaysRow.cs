using Microsoft.EntityFrameworkCore;

namespace NewStock.Models;

/// <summary>
/// dbo.[Strategy突破15日盤整] 之 keyless 結果列（對齊 StockWeb Strategy25）。
/// </summary>
[Keyless]
public sealed class StrategyBreakoutAfter15DaysRow
{
    public required string StockName { get; set; }

    public short StockId { get; set; }

    public DateOnly Date { get; set; }

    public double 漲幅 { get; set; }

    public double 收盤價 { get; set; }

    public long 成交量 { get; set; }

    public double? MA60 { get; set; }

    public double? MA120 { get; set; }

    public long? PrevVolume { get; set; }

    public double? PrevClose { get; set; }

    public int? ConsolidationDays { get; set; }

    public double? ConsolidationMA15 { get; set; }

    public double? MinCloseIn15Days { get; set; }

    public double? MaxCloseIn15Days { get; set; }

    public int? InRangeDays { get; set; }

    public int? PrevDayInRange { get; set; }

    public double? MaxDeviationFromMA15 { get; set; }

    public double? CloseToConsolidationMA15Pct { get; set; }

    public double? BreakoutFromMaxCloseIn15DaysPct { get; set; }
}