using System;
using System.Collections.Generic;

namespace StockWeb.DbModels;

public partial class StockDayInfo
{
    /// <summary>
    /// 測試測試
    /// </summary>
    public DateOnly Date { get; set; }

    public int StockId { get; set; }

    public double 漲幅 { get; set; }

    public double 收盤價 { get; set; }

    public double 開盤價 { get; set; }

    public double 最高價 { get; set; }

    public double 最低價 { get; set; }

    public double 平盤價 { get; set; }

    public int 成交量 { get; set; }

    public int 投信買入 { get; set; }

    public int 投信賣出 { get; set; }

    public int 投信買賣超 { get; set; }

    public int 外資買入 { get; set; }

    public int 外資賣出 { get; set; }

    public int 外資買賣超 { get; set; }

    public double 周轉率 { get; set; }

    public double 當沖率 { get; set; }

    public int 融資買入 { get; set; }

    public int 融資賣出 { get; set; }

    public int 融資買賣超 { get; set; }

    public int 融資餘額 { get; set; }

    public int 融券買入 { get; set; }

    public int 融券賣出 { get; set; }

    public int 融券買賣超 { get; set; }

    public int 融券餘額 { get; set; }

    public int 借券買入 { get; set; }

    public int 借券賣出 { get; set; }

    public int 借券買賣超 { get; set; }

    public int 借券餘額 { get; set; }

    public double 本益比 { get; set; }

    public virtual StockBaseInfo Stock { get; set; } = null!;
}
