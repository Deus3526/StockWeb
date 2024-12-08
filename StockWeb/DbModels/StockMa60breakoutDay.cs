using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class StockMa60breakoutDay
{
    public int StockId { get; set; }

    public DateOnly Date { get; set; }

    public int? DaysSinceBreakout { get; set; }
}
