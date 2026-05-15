using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NewStock.EFModels;

/// <summary>
/// FinMind TaiwanStockTradingDate 對應之交易日清單。
/// </summary>
[Table("TaiwanTradingDay")]
public class TaiwanTradingDay
{
    [Key]
    public DateOnly Date { get; set; }
}