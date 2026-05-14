namespace NewStock.Models.Enum;

/// <summary>
/// 日線資料來源類型（與 StockWeb 對齊：盤後 / 即時）。
/// </summary>
public enum StockDayInfoDataTypeEnum
{
    Unknown = 0,
    盤後 = 1,
    即時 = 2,
}