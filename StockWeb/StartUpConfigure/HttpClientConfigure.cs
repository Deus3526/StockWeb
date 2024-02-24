using Polly;
using Polly.Extensions.Http;
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
            }).AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddMyPollyPolicy();

            builder.Services.AddHttpClient(ConstHttpClinetName.openapiTwse, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.openapiTwse}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            }).AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddMyPollyPolicy();

            builder.Services.AddHttpClient(ConstHttpClinetName.TPEX, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.TPEX}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            }).AddHttpMessageHandler<LoggingDelegatingHandler>()
            .AddMyPollyPolicy();
        }

        public static IHttpClientBuilder AddMyPollyPolicy(this IHttpClientBuilder builder)
        {
            // 使用 AddPolicyHandler 来获取 ILogger 实例
            builder.AddPolicyHandler((serviceProvider, request) =>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<HttpClient>>();

                var retryPolicy = HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .WaitAndRetryAsync(
                        new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(3) },
                        onRetry: (outcome, timespan, retryAttempt, context) =>
                        {
                            logger.LogWarning("請求{request.RequestUri}失敗，{timespan}秒後重試第{retryAttempt}次",request.RequestUri,timespan,retryAttempt);
                        });


                return retryPolicy;
            });
            return builder;
        }

    }


    public class LoggingDelegatingHandler(ILogger<LoggingDelegatingHandler> logger) : DelegatingHandler
    {
        private readonly ILogger<LoggingDelegatingHandler> _logger = logger;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // 日誌記錄請求  httpClient其實自己會寫log(System.Net.Http的information)，如果需要額外寫到檔案紀錄的話，才需要使用自訂的Handler
            //LogRequest(request);

            // 發送 HTTP 請求
            var response = await base.SendAsync(request, cancellationToken);

            // 日誌記錄響應
            //LogResponse(request,response);

            return response;
        }

        private void LogRequest(HttpRequestMessage request)
        {
            request.Options.TryGetValue(new HttpRequestOptionsKey<string?>(ConstString.HttpLogMessage),out string? messageForLog);
            // 实现请求日志逻辑
            if(messageForLog!=null) _logger.LogInformation("Sending request to {messageForLog} : {request.RequestUri}",messageForLog,request.RequestUri);
            else _logger.LogInformation("發送請求至 : {request.RequestUri}",request.RequestUri);
        }

        private void LogResponse(HttpRequestMessage request,HttpResponseMessage response)
        {
            // 实现响应日志逻辑
            _logger.LogInformation("接受到來自 {request.RequestUri} 的回應 with status code {response.StatusCode}",request.RequestUri,response.StatusCode);
        }
    }
}
