using System.Net;
using System.Text;
using System.Text.Json;

namespace NewStockTimer;

/// <summary>
/// 迴圈呼叫 NewStock <c>POST api/Update/UpdateStockDayInfo</c>（日線＋條件週／月 K；分 K 未串接於此 API）。
/// 單次完成並成功後等待 1 分鐘再執行；若回應為 <strong>400</strong> 則停止，請處理後再手動啟動。
/// </summary>
internal static class Program
{
    private static readonly TimeSpan DelayAfterSuccess = TimeSpan.FromMinutes(1);

    private const string UpdateStockDayInfoUrl = "http://localhost:5247/api/Update/UpdateStockDayInfo";

    public static async Task Main(string[] args)
    {
        using var client = new HttpClient();
        Console.WriteLine("週期：成功後等待 1 分鐘再跑下一輪；HTTP 400 時停止。Press Enter 結束…");

        using var exitCts = new CancellationTokenSource();
        _ = Task.Run(
            () =>
            {
                Console.ReadLine();
                exitCts.Cancel();
            },
            exitCts.Token);

        try
        {
            while (!exitCts.Token.IsCancellationRequested)
            {
                Console.WriteLine();
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 呼叫 {UpdateStockDayInfoUrl}");

                using var response = await PostJsonBodyAsync(client, UpdateStockDayInfoUrl, new { }, exitCts.Token)
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync(exitCts.Token).ConfigureAwait(false);
                Console.WriteLine(body);

                if (response.StatusCode == HttpStatusCode.BadRequest)
                {
                    Console.WriteLine();
                    Console.WriteLine("收到 HTTP 400，已停止排程（請處理伺服端／FinMind 後再啟動 Timer）。");
                    break;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine();
                    Console.WriteLine($"HTTP {(int)response.StatusCode}，已停止。");
                    break;
                }

                Console.WriteLine($"{DelayAfterSuccess.TotalMinutes} 分鐘後執行下一輪…");
                try
                {
                    await Task.Delay(DelayAfterSuccess, exitCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("結束。");
        }
    }

    private static Task<HttpResponseMessage> PostJsonBodyAsync(
        HttpClient client,
        string url,
        object data,
        CancellationToken cancellationToken)
    {
        var jsonContent = JsonSerializer.Serialize(data);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        return client.PostAsync(url, content, cancellationToken);
    }
}
