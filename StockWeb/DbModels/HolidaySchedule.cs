using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class HolidaySchedule
{
    public DateOnly Date { get; set; }

    public string? Reason { get; set; }

    public string? Detail { get; set; }
}
