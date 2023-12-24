using StockWeb.ConstData;

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

        public async  Task<T> GetFromJsonAsync<T>(string httpClientName,string routeName,Dictionary<string,string>? queryParams=null)
        {
            ArgumentNullException.ThrowIfNullOrEmpty(httpClientName);
            ArgumentNullException.ThrowIfNullOrEmpty(routeName);
            HttpClient client = _httpClientFactory.CreateClient(httpClientName);
            string route = _config[$"{httpClientName}:{ConstString.Route}:{routeName}"]!;
            route=SetQueryParams(route, queryParams);
            var res= await client.GetFromJsonAsync<T>(route);
            ArgumentNullException.ThrowIfNull(res);
            return res;
        }

        private string SetQueryParams(string route, Dictionary<string, string>? queryParams)
        {
            string queryString = string.Empty;
            if (queryParams != null && queryParams.Count > 0)
            {
                if (route.Contains("?"))
                {
                    queryString += "&";
                }
                else
                {
                    queryString += "?";
                }
                foreach(var kvp in queryParams)
                {
                    queryString += $"{kvp.Key}={kvp.Value}";
                }
            }
            return route+queryString;
        }
    }
}
