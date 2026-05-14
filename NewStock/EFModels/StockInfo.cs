using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NewStock.Models.Enum;

namespace NewStock.EFModels;

[Table("StockInfo")]
public class StockInfo
{
    [Key]
    public short StockId { get; set; }

    [StringLength(128)]
    public string StockName { get; set; } = null!;

    [StringLength(256)]
    public string IndustryCategory { get; set; } = null!;

    [StringLength(32)]
    public MarketTypeEnum MarketType { get; set; }

    public virtual ICollection<StockDayInfo> StockDayInfos { get; set; } = new List<StockDayInfo>();
}
