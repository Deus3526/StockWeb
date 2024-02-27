using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using StockWeb.ConstData;
using StockWeb.Models.ApiResponseModel;

namespace StockWeb.Services
{
    public class RequestApiService(IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
        private readonly IConfiguration _config = config;

        public async Task<T> GetFromJsonAsync<T>(string httpClientName, string route, IDictionary<string, string?>? queryParams=null,string? httpLogMessage=null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(httpClientName);
            ArgumentNullException.ThrowIfNullOrEmpty(route);
            HttpClient client = _httpClientFactory.CreateClient(httpClientName);
            if(queryParams!=null)route = QueryHelpers.AddQueryString(route, queryParams);

            //var res = await client.GetFromJsonAsync<T>(route);
            HttpRequestMessage requestMessage=new(HttpMethod.Get, route);
            requestMessage.Options.Set(new HttpRequestOptionsKey<string?>(ConstString.HttpLogMessage), httpLogMessage);
            var response=await client.SendAsync(requestMessage);
            var res=await response.Content.ReadFromJsonAsync<T>();
            ArgumentNullException.ThrowIfNull(res);
            return res;
        }



    }
}
