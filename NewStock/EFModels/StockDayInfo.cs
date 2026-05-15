using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NewStock.Models.Enum;

namespace NewStock.EFModels;

/// <summary>
/// 個股日線（欄位與 StockWeb 對齊；僅 Date、StockId、DataType 為英文識別）。
/// </summary>
[Table("StockDayInfo")]
public class StockDayInfo
{
    public DateOnly Date { get; set; }

    public short StockId { get; set; }

    [StringLength(32)]
    public StockDayInfoDataTypeEnum DataType { get; set; }

    public double 漲幅 { get; set; }

    public double 收盤價 { get; set; }

    public double 開盤價 { get; set; }

    public double 最高價 { get; set; }

    public double 最低價 { get; set; }

    public double 平盤價 { get; set; }

    /// <summary>張數（或與 StockWeb 對齊之口徑）。</summary>
    public long 成交量 { get; set; }

    [ForeignKey(nameof(StockId))]
    public virtual StockInfo Stock { get; set; } = null!;
}