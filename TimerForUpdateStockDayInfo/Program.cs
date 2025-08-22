using System.Text;
using System.Text.Json;
using Timer = System.Timers.Timer;

namespace TimerForUpdateStockDayInfo
{
    internal class Program
    {
        private const int periodMintue = 3;
        static async Task Main(string[] args)
        {
            HttpClient client = new HttpClient();
            var timer = new Timer(1000 * 60 * periodMintue); // 10分鐘觸發一次
            Console.WriteLine("Press Enter to exit...");
            await AsyncOperation(client, timer);
            timer.Elapsed += async (sender, e) => await AsyncOperation(client, timer);
            timer.Start();


            // 防止程序立即退出

            Console.ReadLine();
        }
        static async Task AsyncOperation(HttpClient client, Timer timer)
        {
            try
            {
                Console.WriteLine($"發出請求，目前時間 : {DateTime.Now}");
                var response = await PostJsonBodyAsync(client, "https://localhost:7192/api/Stock/UpdateStockDayInfo", new { IsHistoricalUpdate = false });
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("取得回應成功");
                }
                else
                {
                    Console.WriteLine($"回應狀態碼失敗，{periodMintue}分鐘後再試一次");
                    //timer.Stop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"發生錯誤，{periodMintue}分鐘後再試一次");
                Console.WriteLine(ex);
                //timer.Stop();
            }
        }

        async static Task<HttpResponseMessage> PostJsonBodyAsync(HttpClient client, string Url, object data)
        {
            // 將 object 序列化為 JSON 字符串
            var jsonContent = JsonSerializer.Serialize(data);

            // 使用 StringContent 封裝 JSON 字符串，並設定 Content-Type 為 application/json
            using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            return await client.PostAsync(Url, content);
        }
    }
}
