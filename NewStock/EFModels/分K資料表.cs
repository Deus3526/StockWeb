using NewStock.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewStock.EFModels;

/// <summary>
/// FinMind TaiwanStockKBar 分鐘 K；資料表 分K資料表。
/// </summary>
/// <remarks>
/// 主鍵為 (StockId, Date, 分鐘)。DataType 不納入主鍵。
/// </remarks>
[Table("分K資料表")]
public class 分K資料表
{
    public DateOnly Date { get; set; }

    public TimeOnly 分鐘 { get; set; }

    public short StockId { get; set; }

    [StringLength(32)]
    public StockDayInfoDataTypeEnum DataType { get; set; }

    public double 開盤價 { get; set; }

    public double 最高價 { get; set; }

    public double 最低價 { get; set; }

    public double 收盤價 { get; set; }

    public long 成交量 { get; set; }

    [ForeignKey(nameof(StockId))]
    public virtual StockInfo Stock { get; set; } = null!;
}