using System.Text;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace NewStockTimer;

/// <summary>
/// 定時呼叫 NewStock：
/// <list type="number">
/// <item><description><c>POST api/Update/UpdateStockDayInfo</c>：寫入下一個交易日之 <c>StockDayInfo</c>；若與上一筆盤後日跨曆週／曆月則連動週 K、月 K。</description></item>
/// <item><description>成功後再以回傳之 <c>tradingDay</c> 呼叫 <c>POST api/Update/UpdateTaiwanStockKBar?date=…</c> 置換該日全部分 K（與日線同日）。</description></item>
/// </list>
/// </summary>
internal class Program
{
    private const int PeriodMinute = 60;

    private const string StockDayInfoUrl = "http://localhost:5247/api/Update/UpdateStockDayInfo";

    private static string TaiwanStockKBarUrl(DateOnly tradingDay) =>
        $"http://localhost:5247/api/Update/UpdateTaiwanStockKBar?date={tradingDay:yyyy-MM-dd}";

    private static readonly JsonSerializerOptions s_jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static async Task Main(string[] args)
    {
        using var client = new HttpClient();
        var timer = new Timer(1000 * 60 * PeriodMinute);
        Console.WriteLine("Press Enter to exit...");
        await AsyncOperation(client).ConfigureAwait(false);
        timer.Elapsed += async (_, _) => await AsyncOperation(client).ConfigureAwait(false);
        timer.Start();

        Console.ReadLine();
        timer.Stop();
    }

    private static async Task AsyncOperation(HttpClient client)
    {
        try
        {
            Console.WriteLine($"發出請求，目前時間 : {DateTime.Now}");
            Console.WriteLine(StockDayInfoUrl);
            using var dayInfoResponse = await PostJsonBodyAsync(client, StockDayInfoUrl, new { }).ConfigureAwait(false);
            var dayBody = await dayInfoResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine(dayBody);

            if (!dayInfoResponse.IsSuccessStatusCode)
            {
                Console.WriteLine($"UpdateStockDayInfo 失敗 {(int)dayInfoResponse.StatusCode}，{PeriodMinute} 分鐘後再試。");
                return;
            }

            var dayResult = JsonSerializer.Deserialize<UpdateStockDayInfoResponse>(dayBody, s_jsonOptions);
            if (dayResult is null)
            {
                Console.WriteLine("無法解析 UpdateStockDayInfo 回應，略過分 K。");
                return;
            }

            var kUrl = TaiwanStockKBarUrl(dayResult.TradingDay);
            Console.WriteLine($"接著更新分 K（同日 tradingDay）：{kUrl}");
            using var kResponse = await PostJsonBodyAsync(client, kUrl, new { }).ConfigureAwait(false);
            var kBody = await kResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            Console.WriteLine(kBody);

            if (!kResponse.IsSuccessStatusCode)
                Console.WriteLine($"UpdateTaiwanStockKBar 失敗 {(int)kResponse.StatusCode}（日線與週／月 K 可能已成功）；{PeriodMinute} 分鐘後再試。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"發生錯誤，{PeriodMinute} 分鐘後再試一次");
            Console.WriteLine(ex);
        }
    }

    private static Task<HttpResponseMessage> PostJsonBodyAsync(HttpClient client, string url, object data)
    {
        var jsonContent = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        return client.PostAsync(url, content);
    }

    private sealed class UpdateStockDayInfoResponse
    {
        public DateOnly TradingDay { get; set; }
    }
}
