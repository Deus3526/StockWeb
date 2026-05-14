using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace NewStock.EFModels;

[Table("StockInfo")]
public partial class StockInfo
{
    [Key]
    public short StockId { get; set; }

    [StringLength(128)]
    public string StockName { get; set; } = null!;

    [StringLength(256)]
    public string IndustryCategory { get; set; } = null!;

    [StringLength(32)]
    public string MarketType { get; set; } = null!;
}
