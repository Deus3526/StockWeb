
using Serilog;
using StockWeb.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ExceptionServices;

namespace StockWeb.StartUpConfigure.Middleware
{
    public class RequestLogMiddleware(ILogger<RequestLogMiddleware> logger, IHostEnvironment env) : IMiddleware
    {
        private readonly ILogger<RequestLogMiddleware> _logger = logger;
        private readonly IHostEnvironment _env = env;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var start = DateTimeOffset.UtcNow;
            
            // 繼續管道中的其他中介軟體
            await next(context);

            // 記錄Request跟Response，即使發生了異常也會執行
            LogResponse(context, start);
        }

        private void LogResponse(HttpContext context, DateTimeOffset start)
        {
            var end = DateTimeOffset.UtcNow; // 獲取當前 UTC 時間
            var duration = end - start; // 計算持續時間，先使用UTC來計算時間，精確度會比較高
            var request = context.Request;
            var response=context.Response;
            var requestInfo = new
            {
                Method = request.Method,
                Path = request.Path,
                QueryString = request.QueryString.ToString(),
                TimeStamp = start.ToLocalTime()
            };
            var responseInfo = new
            {
                StatusCode = response.StatusCode,
                Duration = duration.TotalMilliseconds,
                Timestamp=end.ToLocalTime()
            };

            _logger.LogInformation($"HTTP Request-Response:{{@RequestInfo}}-{{@ResponseInfo}}-{{@{nameof(LogTypeEnum)}}}", requestInfo,responseInfo,LogTypeEnum.Request);
        }
    }


    public static class RequestLogMiddlewareExtension
    {
        /// <summary>
        /// 使用Middlware統一紀錄Request的log
        /// </summary>
        /// <param name="app"></param>
        public static void UseRequestLogMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestLogMiddleware>();
        }
    }
}
