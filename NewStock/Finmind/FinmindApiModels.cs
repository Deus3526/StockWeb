using NewStock.Models.Enum;
using System.Globalization;
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
/// FinMind <c>taiwan_stock_tick_snapshot</c> 請求結果：對方原始 <c>data</c> 列數，以及經合法性篩選（含本機今日曆日）後、同一 <c>stock_id</c> 僅保留 <see cref="TaiwanStockTickSnapshotResponse.SnapshotInstant"/> 最晚之一筆的列表。
/// </summary>
public sealed record TaiwanStockTickSnapshotApiResult(int ApiRawRowCount, IReadOnlyList<TaiwanStockTickSnapshotResponse> Rows);

/// <summary>
/// FinMind <c>taiwan_stock_tick_snapshot</c>（盤中即時／快照）單列；見 <c>Finmind/ApiTest/即時資料.http</c>。
/// </summary>
public sealed class TaiwanStockTickSnapshotResponse : BaseStockResponse
{
    [JsonPropertyName("open")]
    public double Open { get; set; }

    [JsonPropertyName("high")]
    public double High { get; set; }

    [JsonPropertyName("low")]
    public double Low { get; set; }

    [JsonPropertyName("close")]
    public double Close { get; set; }

    /// <summary>
    /// 漲跌金額（元）；語意對齊「現價相對昨日參考（平盤基準）之價差」時，等同日線之 <see cref="TaiwanStockPriceResponse.Spread"/>，故 <see cref="平盤價"/> 取 <see cref="Close"/> 減本欄位。
    /// </summary>
    [JsonPropertyName("change_price")]
    public double ChangePrice { get; set; }

    /// <summary>
    /// 漲跌百分比（文件註為 <c>%</c>）；轉為與日線相同之「比率」請用 <see cref="漲跌幅比率"/>。
    /// </summary>
    [JsonPropertyName("change_rate")]
    public double ChangeRate { get; set; }

    [JsonPropertyName("average_price")]
    public double AveragePrice { get; set; }

    /// <summary>單筆成交量（股）。</summary>
    [JsonPropertyName("volume")]
    public long Volume { get; set; }

    /// <summary>累積成交量（張）；寫入 <see cref="EFModels.StockDayInfo"/>。</summary>
    [JsonPropertyName("total_volume")]
    public long TotalVolume { get; set; }

    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("total_amount")]
    public long TotalAmount { get; set; }

    [JsonPropertyName("yesterday_volume")]
    public long YesterdayVolume { get; set; }

    [JsonPropertyName("buy_price")]
    public double BuyPrice { get; set; }

    [JsonPropertyName("buy_volume")]
    public long BuyVolume { get; set; }

    [JsonPropertyName("sell_price")]
    public double SellPrice { get; set; }

    [JsonPropertyName("sell_volume")]
    public long SellVolume { get; set; }

    [JsonPropertyName("volume_ratio")]
    public double VolumeRatio { get; set; }

    /// <summary>
    /// FinMind 回應鍵名 <c>date</c>（成交時間字串，例如 <c>2026-05-18 09:13:01.247135</c>）。
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// 自 <see cref="Date"/> 以 Invariant 解析之成交時間；空白或無法 Parse 時為 <see langword="null"/>。
    /// </summary>
    [JsonIgnore]
    public DateTime? SnapshotInstant =>
        TryParseSnapshotInstant(Date, out var instant) ? instant : null;

    /// <summary>
    /// 嘗試解析 FinMind 即時快照 <c>date</c> 字串（與 <see cref="SnapshotInstant"/> 相同規則）。
    /// </summary>
    public static bool TryParseSnapshotInstant(string? raw, out DateTime instant)
    {
        instant = default;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return DateTime.TryParse(
            raw.Trim(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal,
            out instant);
    }

    /// <summary>
    /// 與 <see cref="TaiwanStockPriceResponse.漲幅"/> 相同語意：小數形式之漲跌幅（例如 0.01 表示 1%）。
    /// FinMind 之 <c>change_rate</c> 文件為百分比數字（0.29 表示 0.29%）。
    /// </summary>
    public double 漲跌幅比率 => ChangeRate / 100.0;

    /// <summary>
    /// 參考價／昨日收盤之平盤基準（元）；與 <see cref="TaiwanStockPriceResponse.平盤價"/> 相同：<c>Close - 價差</c>。
    /// </summary>
    [JsonIgnore]
    public double 平盤價 => Close - ChangePrice;
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
