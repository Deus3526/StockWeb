using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class DividendYield
{
    public int StockId { get; set; }

    public DateOnly PayDate { get; set; }

    public double DividendYieldRate { get; set; }

    public double Payment { get; set; }

    public virtual StockBaseInfo Stock { get; set; } = null!;
}
