using StockWeb.ConstData;

namespace StockWeb.StartUpConfigure
{
    public static class HttpClientConfigurator
    {
        public static void HttpClientConfigure(this WebApplicationBuilder builder)
        {
            var config = builder.Configuration;
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<LoggingDelegatingHandler>();
            builder.Services.AddHttpClient(ConstHttpClinetName.TWSE, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.TWSE}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            }).AddHttpMessageHandler<LoggingDelegatingHandler>();

            builder.Services.AddHttpClient(ConstHttpClinetName.openapiTwse, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.openapiTwse}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            }).AddHttpMessageHandler<LoggingDelegatingHandler>();

            builder.Services.AddHttpClient(ConstHttpClinetName.TPEX, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.TPEX}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            }).AddHttpMessageHandler<LoggingDelegatingHandler>();
        }
    }


    public class LoggingDelegatingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger;
        public LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger)
        {
            _logger = logger;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 日誌記錄請求
            LogRequest(request);

            // 發送 HTTP 請求
            var response = await base.SendAsync(request, cancellationToken);

            // 日誌記錄響應
            LogResponse(request,response);

            return response;
        }

        private void LogRequest(HttpRequestMessage request)
        {
            request.Options.TryGetValue(new HttpRequestOptionsKey<string?>(ConstString.HttpLogMessage),out string? messageForLog);
            // 实现请求日志逻辑
            if(messageForLog!=null) _logger.LogInformation($"Sending request to {messageForLog} : {request.RequestUri}");
            else _logger.LogInformation($"Sending request to : {request.RequestUri}");
        }

        private void LogResponse(HttpRequestMessage request,HttpResponseMessage response)
        {
            // 实现响应日志逻辑
            _logger.LogInformation($"Received response of {request.RequestUri} with status code {response.StatusCode}");
        }
    }
}
