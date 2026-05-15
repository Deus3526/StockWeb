using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using NewStock.Models.Enum;
using System.Globalization;
using System.Net.Http.Headers;

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
    /// 同一 <see cref="BaseStockResponse.StockIdShort"/> 若多筆，只保留 <see cref="TaiwanStockInfoResponse.Date"/> 最大的一筆（無效／缺失日期於 JSON 反序列化為 <see cref="DateOnly.MinValue"/>）。
    /// </para>
    /// </summary>
    public async Task<IReadOnlyList<TaiwanStockInfoResponse>> GetTaiwanStockInfoAsync(CancellationToken cancellationToken)
    {
        var url = QueryHelpers.AddQueryString(
            $"{_configFinmind.Domain}/data",
            new Dictionary<string, string?> { ["dataset"] = "TaiwanStockInfo" });
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse = await response.Content.ReadFromJsonAsync<FinmindBaseResponse<List<TaiwanStockInfoResponse>>>(cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockInfo 回應或缺少 data。");

        return baseResponse.Data
            .Where(x => x.MarketTypeEnum != MarketTypeEnum.Unknown && x.IsEligibleStock())
            .GroupBy(r => r.StockIdShort)
            .Select(g => g.OrderByDescending(r => r.Date).First())
            .ToList();
    }

    /// <summary>
    /// TaiwanStockPrice：不分 <c>data_id</c>（單一交易日）。
    /// <para>若 FinMind 帳號需 Token，請在設定 <see cref="FinmindConfig.Token"/>；否則可能無法取得成功回應。</para>
    /// <para>
    /// 僅回傳：<see cref="TaiwanStockPriceResponse.Date"/> 有效且等於 <paramref name="tradingDay"/>、且 <see cref="BaseStockResponse.IsEligibleStock"/> 之列（與 <see cref="GetTaiwanStockInfoAsync"/> 之代號篩選對齊）。
    /// </para>
    /// </summary>
    public async Task<IReadOnlyList<TaiwanStockPriceResponse>> GetTaiwanStockPriceForTradingDayAsync(DateOnly tradingDay, CancellationToken cancellationToken)
    {
        var url = QueryHelpers.AddQueryString(
            $"{_configFinmind.Domain}/data",
            new Dictionary<string, string?>
            {
                ["dataset"] = "TaiwanStockPrice",
                ["start_date"] = tradingDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["end_date"] = tradingDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _configFinmind.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse =
            await response.Content.ReadFromJsonAsync<FinmindBaseResponse<List<TaiwanStockPriceResponse>>>(
                cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockPrice 回應或缺少 data。");

        return baseResponse.Data
            .Where(x => x.Date != DateOnly.MinValue && x.Date == tradingDay && x.IsEligibleStock())
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

        var url = QueryHelpers.AddQueryString(
            $"{_configFinmind.Domain}/data",
            new Dictionary<string, string?>
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

    /// <summary>
    /// TaiwanStockWeekPrice：<paramref name="weekStartMonday"/> 須為該根 K 對應之「週一」；<c>start_date</c>／<c>end_date</c> 同該日以取得單一週區間。
    /// 僅回傳：<see cref="TaiwanStockWeekMonthPriceResponse.Date"/> 等於 <paramref name="weekStartMonday"/>，且通過代號篩選之列。
    /// </summary>
    public async Task<IReadOnlyList<TaiwanStockWeekMonthPriceResponse>> GetTaiwanStockWeekPriceAsync(
        DateOnly weekStartMonday,
        CancellationToken cancellationToken)
    {
        var dateStr = weekStartMonday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = QueryHelpers.AddQueryString(
            $"{_configFinmind.Domain}/data",
            new Dictionary<string, string?>
            {
                ["dataset"] = "TaiwanStockWeekPrice",
                ["start_date"] = dateStr,
                ["end_date"] = dateStr,
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configFinmind.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse =
            await response.Content.ReadFromJsonAsync<
                FinmindBaseResponse<List<TaiwanStockWeekMonthPriceResponse>>>(cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockWeekPrice 回應或缺少 data。");

        return baseResponse.Data
            .Where(x => x.Date != DateOnly.MinValue && x.Date == weekStartMonday && x.IsEligibleStock())
            .ToList();
    }

    /// <summary>
    /// TaiwanStockMonthPrice：<paramref name="monthFirstDay"/> 須為該月一日；<c>start_date</c>／<c>end_date</c> 同該日以取得單月區間。
    /// 僅回傳：<see cref="TaiwanStockWeekMonthPriceResponse.Date"/> 等於 <paramref name="monthFirstDay"/>，且通過代號篩選之列。
    /// </summary>
    public async Task<IReadOnlyList<TaiwanStockWeekMonthPriceResponse>> GetTaiwanStockMonthPriceAsync(
        DateOnly monthFirstDay,
        CancellationToken cancellationToken)
    {
        var dateStr = monthFirstDay.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = QueryHelpers.AddQueryString(
            $"{_configFinmind.Domain}/data",
            new Dictionary<string, string?>
            {
                ["dataset"] = "TaiwanStockMonthPrice",
                ["start_date"] = dateStr,
                ["end_date"] = dateStr,
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _configFinmind.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse =
            await response.Content.ReadFromJsonAsync<
                FinmindBaseResponse<List<TaiwanStockWeekMonthPriceResponse>>>(cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockMonthPrice 回應或缺少 data。");

        return baseResponse.Data
            .Where(x => x.Date != DateOnly.MinValue && x.Date == monthFirstDay && x.IsEligibleStock())
            .ToList();
    }

    /// <summary>
    /// TaiwanStockKBar：<paramref name="date"/> 為單一交易日，<paramref name="stockId"/> 為 <c>data_id</c>（四位代號）；須帶 Bearer。
    /// 僅回傳：<see cref="TaiwanStockKBarResponse.Date"/> 等於 <paramref name="date"/>、且 <see cref="BaseStockResponse.StockIdShort"/> 等於 <paramref name="stockId"/> 之列。
    /// </summary>
    public async Task<IReadOnlyList<TaiwanStockKBarResponse>> GetTaiwanStockKBarAsync(
        DateOnly date,
        short stockId,
        CancellationToken cancellationToken)
    {
        var dateStr = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var four = stockId.ToString("D4", CultureInfo.InvariantCulture);
        var url = QueryHelpers.AddQueryString(
            $"{_configFinmind.Domain}/data",
            new Dictionary<string, string?>
            {
                ["dataset"] = "TaiwanStockKBar",
                ["start_date"] = dateStr,
                ["data_id"] = four,
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _configFinmind.Token);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var baseResponse =
            await response.Content.ReadFromJsonAsync<
                FinmindBaseResponse<List<TaiwanStockKBarResponse>>>(cancellationToken);

        if (baseResponse is null || baseResponse.Data is null)
            throw new InvalidOperationException("無法解析 FinMind TaiwanStockKBar 回應或缺少 data。");

        return baseResponse.Data
            .Where(x =>
                x.Date != DateOnly.MinValue
                && x.Date == date
                && x.StockIdShort == stockId)
            .ToList();
    }
}
