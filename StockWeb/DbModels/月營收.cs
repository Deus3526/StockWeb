using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class 月營收
{
    public long Id { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }

    public int StockId { get; set; }

    public double MoM { get; set; }

    public double YoY { get; set; }

    public double 累計yoY { get; set; }
}
