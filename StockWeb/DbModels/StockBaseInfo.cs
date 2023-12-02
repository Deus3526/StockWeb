﻿using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class StockBaseInfo
{
    public int StockId { get; set; }

    public string StockName { get; set; } = null!;

    //public string StockType { get; set; } = null!;

    public int StockAmount { get; set; }

    public string? Category { get; set; }
}
