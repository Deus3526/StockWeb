using Microsoft.AspNetCore.WebUtilities;
using StockWeb.ConstData;
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

    }
}
