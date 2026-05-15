using Microsoft.Extensions.Options;
using NewStock.Models.Enum;

namespace NewStock.Finmind;

public sealed class FinmindApiClient
{
    private readonly HttpClient _httpClient;
    private readonly FinmindConfig _configFinmind;

    public FinmindApiClient(HttpClient httpClient, IOptions<FinmindConfig> options)
    {
        _httpClient = httpClient;
        _configFinmind = options.Value;
    }

    /// <summary>
    /// TaiwanStockInfo：過濾為上市／上櫃且 stock_id 長度為 4。
    /// <para>
    /// 同一 <see cref="TaiwanStockInfoResponse.StockIdShort"/> 若多筆，只保留 <see cref="TaiwanStockInfoResponse.Date"/> 最大的一筆（無效／缺失日期於 JSON 反序列化為 <see cref="DateOnly.MinValue"/>）。
    /// </para>
    /// </summary>
    public async Task<IReadOnlyList<TaiwanStockInfoResponse>> GetTaiwanStockInfoAsync(CancellationToken cancellationToken)
    {
        var url = BuildDataUrl(new Dictionary<string, string?>
        {
            ["dataset"] = "TaiwanStockInfo",
        });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse = await response.Content.ReadFromJsonAsync<FinmindBaseResponse<List<TaiwanStockInfoResponse>>>(cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockInfo 回應或缺少 data。");

        return baseResponse.Data
            .Where(x => x.MarketTypeEnum != MarketTypeEnum.Unknown && x.StockIdShort >= 1000)
            .GroupBy(r => r.StockIdShort)
            .Select(g => g.OrderByDescending(r => r.Date).First())
            .ToList();
    }
    /// <summary>
    /// TaiwanStockTradingDate：<c>start_date</c>～<c>end_date</c>（含）；回傳區間內有效、去重後之日期（與 ApiTest，不帶 Bearer）。
    /// </summary>
    public async Task<IReadOnlyList<DateOnly>> GetTaiwanStockTradingDatesAsync(
        DateOnly startDate,
        DateOnly endDate,
        CancellationToken cancellationToken)
    {
        if (endDate < startDate)
            throw new ArgumentOutOfRangeException(nameof(endDate), "endDate 不得小於 startDate。");

        var url = BuildDataUrl(new Dictionary<string, string?>
        {
            ["dataset"] = "TaiwanStockTradingDate",
            ["start_date"] = startDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["end_date"] = endDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        });

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse =
            await response.Content.ReadFromJsonAsync<FinmindBaseResponse<List<TaiwanStockTradingDateResponse>>>(
                cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockTradingDate 回應或缺少 data。");

        return baseResponse.Data
            .Select(r => r.Date)
            .Where(d => d != DateOnly.MinValue && d >= startDate && d <= endDate)
            .Distinct()
            .ToList();
    }
}
