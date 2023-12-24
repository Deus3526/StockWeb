using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using StockWeb.ConstData;
using StockWeb.Models.ApiResponseModel;

namespace StockWeb.Services
{
    public class RequestApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        public RequestApiService(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        public async Task<T> GetFromJsonAsync<T>(string httpClientName, string routeName, IDictionary<string, string?>? queryParams=null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(httpClientName);
            ArgumentNullException.ThrowIfNullOrEmpty(routeName);
            HttpClient client = _httpClientFactory.CreateClient(httpClientName);
            string route = _config[$"{httpClientName}:{ConstString.Route}:{routeName}"]!;
            if(queryParams!=null)route = QueryHelpers.AddQueryString(route, queryParams);

            var res = await client.GetFromJsonAsync<T>(route);
            ArgumentNullException.ThrowIfNull(res);
            return res;
        }



    }
}
