using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using StockWeb.ConstData;
using StockWeb.Models.ApiResponseModel;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace StockWeb.Services
{
    public enum HttpContentType
    {
        Json,
        FormData,
        UrlEncoded
    }

    public class RequestApiService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IConfiguration _config = config;

        public async Task<T> GetFromJsonAsync<T>(string httpClientName, string route, IDictionary<string, string?>? queryParams = null, string? httpLogMessage = null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(httpClientName);
            ArgumentNullException.ThrowIfNullOrEmpty(route);
            HttpClient client = _httpClientFactory.CreateClient(httpClientName);
            if (queryParams != null) route = QueryHelpers.AddQueryString(route, queryParams);

            //var res = await client.GetFromJsonAsync<T>(route);
            HttpRequestMessage requestMessage = new(HttpMethod.Get, route);
            requestMessage.Options.Set(new HttpRequestOptionsKey<string?>(ConstString.HttpLogMessage), httpLogMessage);
            var response = await client.SendAsync(requestMessage);
            var res = await response.Content.ReadFromJsonAsync<T>();
            ArgumentNullException.ThrowIfNull(res);
            return res;
        }

        public async Task<T> PostFromJsonAsync<T>(
            string httpClientName,
            string route,
            object? body = null,
            HttpContentType contentType = HttpContentType.Json,
            string? httpLogMessage = null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(httpClientName);
            ArgumentNullException.ThrowIfNullOrEmpty(route);

            HttpClient client = _httpClientFactory.CreateClient(httpClientName);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, route);

            // 處理 request body
            if (body != null)
            {
                requestMessage.Content = contentType switch
                {
                    HttpContentType.Json => new StringContent(
                        JsonSerializer.Serialize(body),
                        Encoding.UTF8,
                        "application/json"),

                    HttpContentType.FormData => CreateFormDataContent(body),

                    HttpContentType.UrlEncoded => CreateUrlEncodedContent(body),

                    _ => throw new ArgumentException($"Unsupported content type: {contentType}")
                };
            }

            // 設定 logging message
            if (!string.IsNullOrEmpty(httpLogMessage))
            {
                requestMessage.Options.Set(new HttpRequestOptionsKey<string?>(ConstString.HttpLogMessage), httpLogMessage);
            }

            // 發送請求並處理回應
            var response = await client.SendAsync(requestMessage);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<T>();
            ArgumentNullException.ThrowIfNull(result);

            return result;
        }

        private static HttpContent CreateFormDataContent(object data)
        {
            var formData = new MultipartFormDataContent();
            switch (data)
            {
                case IDictionary<string, string> dict:
                    foreach (var kvp in dict)
                    {
                        formData.Add(new StringContent(kvp.Value), kvp.Key);
                    }
                    break;

                case IDictionary<string, object> objDict:
                    foreach (var kvp in objDict)
                    {
                        if (kvp.Value != null)
                        {
                            formData.Add(new StringContent(kvp.Value.ToString()!), kvp.Key);
                        }
                    }
                    break;

                default:
                    var properties = data.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(data)?.ToString();
                        if (value != null)
                        {
                            formData.Add(new StringContent(value), prop.Name);
                        }
                    }
                    break;
            }

            return formData;
        }

        private static HttpContent CreateUrlEncodedContent(object data)
        {
            var keyValuePairs = new List<KeyValuePair<string, string>>();

            switch (data)
            {
                case IDictionary<string, string> dict:
                    keyValuePairs.AddRange(dict.Select(kvp =>
                        new KeyValuePair<string, string>(kvp.Key, kvp.Value)));
                    break;

                case IDictionary<string, object> objDict:
                    keyValuePairs.AddRange(objDict
                        .Where(kvp => kvp.Value != null)
                        .Select(kvp => new KeyValuePair<string, string>(
                            kvp.Key,
                            kvp.Value.ToString()!)));
                    break;

                default:
                    var properties = data.GetType().GetProperties();
                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(data)?.ToString();
                        if (value != null)
                        {
                            keyValuePairs.Add(new KeyValuePair<string, string>(prop.Name, value));
                        }
                    }
                    break;
            }

            return new FormUrlEncodedContent(keyValuePairs);
        }

        public async Task Get月營收(DateOnly date)
        {
            var url = "https://mops.twse.com.tw/server-java/FileDownLoad";
            var formData = CreateUrlEncodedContent(new
            {
                step = @"9",
                functionName = @"show_file2",
                filePath = @"/t21/sii/",
                fileName = @"t21sc03_113_12.csv"
            });
            var response = await _httpClientFactory.CreateClient().PostAsync(url, formData);
            //var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            // 3. 準備 CsvConfiguration
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",                      // 如果 MOPS CSV 是逗號分隔
                HasHeaderRecord = true,               // 第一行是標頭
                Encoding = Encoding.GetEncoding("big5"), // MOPS 通常是 big5 或 cp950
                IgnoreBlankLines = true,
                MissingFieldFound = null,             // 遇到缺欄位就不 Throw
                BadDataFound = null                  // 遇到壞資料也不 Throw
            };

            // 4. 使用 CsvHelper 解析
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.GetEncoding("big5"));
            using var csv = new CsvReader(reader, config);

            // 這裡可以直接使用屬性 (Attribute) Name() 來對應欄位
            var records = csv.GetRecords<月營收資訊>();
            var list = new List<月營收資訊>(records);

            // 簡單列印幾筆
            foreach (var item in list)
            {
                Console.WriteLine(
                    $"{item.MonthString} / {item.StockId} / MOM月增: {item.MOM月增率}, YoY年增: {item.YoY年增率}, 累計YoY: {item.累計Yoy}"
                );
            }

            // 6. 你可以把 list 寫入資料庫 (EF Core / ADO.NET / Dapper ...)
            // using (var db = new MyDbContext())
            // {
            //     db.月營收資訊表.AddRange(list);
            //     db.SaveChanges();
            // }

        }

    }
}
