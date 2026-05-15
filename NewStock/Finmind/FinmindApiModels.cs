using System.Globalization;
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

public sealed class TaiwanStockInfoResponse
{
    [JsonPropertyName("stock_id")]
    public string? StockId { get; set; }

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
            _ => MarketTypeEnum.Unknown,
        };

    /// <summary>
    /// stock_id 轉為 <see cref="short"/>（無法解析為 0）；使用 <see cref="CultureInfo.InvariantCulture"/>。
    /// </summary>
    [JsonIgnore]
    public short StockIdShort =>
        short.TryParse(StockId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : (short)0;
/// <summary>
/// FinMind TaiwanStockTradingDate（台股交易日）單列。
/// </summary>
public sealed class TaiwanStockTradingDateResponse
{
    [JsonPropertyName("date")]
    [JsonConverter(typeof(SaveDateOnlyJsonConverter))]
    public DateOnly Date { get; set; }
}