using System.Globalization;
using System.Text.Json.Serialization;

namespace NewStock.Finmind;

/// <summary>
/// 含 <c>stock_id</c> 之 FinMind 列基底：代號轉 <see cref="short"/> 與是否納入本系統（四位數上市／上櫃代號區間）。
/// </summary>
public abstract class BaseStockResponse : IBaseStockResponse
{
    [JsonPropertyName("stock_id")]
    public string? StockId { get; set; }

    /// <summary>
    /// <see cref="StockId"/> 轉為 <see cref="short"/>（無法解析為 0）；使用 <see cref="CultureInfo.InvariantCulture"/>。
    /// </summary>
    [JsonIgnore]
    public short StockIdShort =>
        short.TryParse(StockId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id) ? id : (short)0;

    /// <summary>
    /// 四位代號且數值介於 1000–9999（上市／上櫃慣用區間）。
    /// </summary>
    public bool IsEligibleStock() =>
        StockId?.Length == 4 && StockIdShort >= 1000 && StockIdShort <= 9999;
}