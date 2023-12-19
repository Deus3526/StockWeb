using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class MarketDayInfo
{
    public DateOnly Date { get; set; }

    public int 成交張數 { get; set; }

    public long 成交金額 { get; set; }

    public int 成交筆數 { get; set; }

    public double 大盤指數 { get; set; }

    public double 漲跌 { get; set; }
}
