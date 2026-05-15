namespace NewStock.Models.Enum;

/// <summary>
/// 週／月等 K 棒彙總的時間維度（對應 FinMind TaiwanStockWeekPrice / TaiwanStockMonthPrice）；存庫為英文 <c>Week</c>／<c>Month</c>。
/// </summary>
public enum StockKBarTimeTypeEnum
{
    Unknown = 0,
    Week = 1,
    Month = 2,
}
