using NewStock.Models.Enum;
using System.Text.Json.Serialization;

namespace NewStock.Finmind;

/// <summary>
/// FinMind v4 /data 共通 JSON 結構（msg、status、data）。
/// </summary>
public sealed class FinmindBaseResponse<T>
{
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class TaiwanStockInfoResponse : BaseStockResponse
{
    [JsonPropertyName("stock_name")]
    public string? StockName { get; set; }

    [JsonPropertyName("industry_category")]
    public string? IndustryCategory { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 多為 yyyy-MM-dd；JSON null、空白或無法解析為日期時為 <see cref="DateOnly.MinValue"/>（見 <see cref="SaveDateOnlyJsonConverter"/>）。
    /// </summary>
    [JsonPropertyName("date")]
    [JsonConverter(typeof(SaveDateOnlyJsonConverter))]
    public DateOnly Date { get; set; }

    [JsonIgnore]
    public MarketTypeEnum MarketTypeEnum =>
        Type?.Trim().ToLowerInvariant() switch
        {
            "twse" => MarketTypeEnum.上市,
            "tpex" => MarketTypeEnum.上櫃,
            "emerging" => MarketTypeEnum.興櫃,
            _ => MarketTypeEnum.Unknown,
        };

}

/// <summary>
/// FinMind TaiwanStockPrice（股價日成交）單列。
/// </summary>
public sealed class TaiwanStockPriceResponse : BaseStockResponse
{
    [JsonPropertyName("date")]
    [JsonConverter(typeof(SaveDateOnlyJsonConverter))]
    public DateOnly Date { get; set; }

    [JsonPropertyName("Trading_Volume")]
    public long TradingVolume { get; set; }

    [JsonPropertyName("Trading_money")]
    public long TradingMoney { get; set; }

    [JsonPropertyName("open")]
    public double Open { get; set; }

    [JsonPropertyName("max")]
    public double High { get; set; }

    [JsonPropertyName("min")]
    public double Low { get; set; }

    [JsonPropertyName("close")]
    public double Close { get; set; }

    /// <summary>
    /// FinMind <c>spread</c>，此處先假設為「收盤與前一交易日參考價（平盤基準）之差」以利推算平盤價／漲幅；若語意不符再調整。
    /// </summary>
    [JsonPropertyName("spread")]
    public double Spread { get; set; }

    /// <summary>
    /// 依目前 <see cref="Close"/>、<see cref="Spread"/> 推算「參考價／平盤基準」（元）；見 <see cref="Spread"/> 語意假設。不會序列化。
    /// </summary>
    [JsonIgnore]
    public double 平盤價 => Close - Spread;

    /// <summary>
    /// 依目前 <see cref="Close"/>、<see cref="Spread"/> 推算漲跌幅（％）。不會序列化。
    /// </summary>
    [JsonIgnore]
    public double 漲幅 => 平盤價 > 0 ? Spread / 平盤價 : 0;

    /// <summary>成交筆數；FinMind 欄位名為 <c>Trading_turnover</c>。</summary>
    [JsonPropertyName("Trading_turnover")]
    public long TradingTurnover { get; set; }
}

/// <summary>
/// FinMind TaiwanStockWeekPrice／TaiwanStockMonthPrice 共通欄位（週 K 有 <c>yweek</c>、月 K 有 <c>ymonth</c>）。
/// </summary>
public sealed class TaiwanStockWeekMonthPriceResponse : BaseStockResponse
{
    [JsonPropertyName("yweek")]
    public string? Yweek { get; set; }

    [JsonPropertyName("ymonth")]
    public string? Ymonth { get; set; }

    [JsonPropertyName("date")]
    [JsonConverter(typeof(SaveDateOnlyJsonConverter))]
    public DateOnly Date { get; set; }

    [JsonPropertyName("open")]
    public double Open { get; set; }

    [JsonPropertyName("max")]
    public double High { get; set; }

    [JsonPropertyName("min")]
    public double Low { get; set; }

    [JsonPropertyName("close")]
    public double Close { get; set; }

    [JsonPropertyName("spread")]
    public double Spread { get; set; }

    /// <summary>
    /// 依目前 <see cref="Close"/>、<see cref="Spread"/> 推算「參考價／平盤基準」（元）；與 <see cref="TaiwanStockPriceResponse"/> 相同假設。不會序列化。
    /// </summary>
    [JsonIgnore]
    public double 平盤價 => Close - Spread;

    /// <summary>
    /// 依目前 <see cref="Close"/>、<see cref="Spread"/> 推算漲跌幅（％以小數計）。不會序列化。
    /// </summary>
    [JsonIgnore]
    public double 漲幅 => 平盤價 > 0 ? Spread / 平盤價 : 0;

    [JsonPropertyName("trading_turnover")]
    public long TradingTurnover { get; set; }
}

/// <summary>
/// FinMind TaiwanStockKBar（分 K）單列；一日、一檔查詢。
/// </summary>
public sealed class TaiwanStockKBarResponse : BaseStockResponse
{
    [JsonPropertyName("date")]
    [JsonConverter(typeof(SaveDateOnlyJsonConverter))]
    public DateOnly Date { get; set; }

    /// <summary>如 <c>09:00:00</c>。</summary>
    [JsonPropertyName("minute")]
    public string? Minute { get; set; }

    [JsonPropertyName("open")]
    public double Open { get; set; }

    [JsonPropertyName("high")]
    public double High { get; set; }

    [JsonPropertyName("low")]
    public double Low { get; set; }

    [JsonPropertyName("close")]
    public double Close { get; set; }

    [JsonPropertyName("volume")]
    public long Volume { get; set; }
}

/// <summary>
/// FinMind TaiwanStockTradingDate（台股交易日）單列。
/// </summary>
public sealed class TaiwanStockTradingDateResponse
{
    [JsonPropertyName("date")]
    [JsonConverter(typeof(SaveDateOnlyJsonConverter))]
    public DateOnly Date { get; set; }
}
