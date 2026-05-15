using NewStock.Models.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewStock.EFModels;

/// <summary>
/// 台股週／月 K 彙總（FinMind <c>trading_turnover</c> → <see cref="交易筆數"/>）。
/// </summary>
/// <remarks>
/// 主鍵為 (<see cref="StockId"/>, <see cref="Date"/>, <see cref="TimeType"/>)。
/// <see cref="DataType"/> 僅區分目前資料為即時或盤後，不與其他維度同時重複存成兩列。
/// 若僅用 (<see cref="StockId"/>, <see cref="Date"/>) 仍可能使週 K 與月 K 在「月初為週一」時撞日，故保留 <see cref="TimeType"/>。
/// </remarks>
[Table("周月K資料表")]
public class 周月K資料表
{
    public DateOnly Date { get; set; }

    public short StockId { get; set; }

    [StringLength(32)]
    public StockKBarTimeTypeEnum TimeType { get; set; }

    [StringLength(32)]
    public StockDayInfoDataTypeEnum DataType { get; set; }

    public double 開盤價 { get; set; }

    public double 最高價 { get; set; }

    public double 最低價 { get; set; }

    public double 收盤價 { get; set; }

    /// <summary>與日 K 相同：依 FinMind <c>close</c>、<c>spread</c> 推算（收盤 − spread）。</summary>
    public double 平盤價 { get; set; }

    /// <summary>與日 K 相同：漲跌幅（小數，例如 0.01 即 1%）。</summary>
    public double 漲幅 { get; set; }

    /// <summary>FinMind <c>trading_turnover</c>。</summary>
    public long 交易筆數 { get; set; }

    [ForeignKey(nameof(StockId))]
    public virtual StockInfo Stock { get; set; } = null!;
}
