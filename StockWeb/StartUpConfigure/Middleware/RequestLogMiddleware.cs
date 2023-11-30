
using Serilog;
using StockWeb.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.ExceptionServices;

namespace StockWeb.StartUpConfigure.Middleware
{
    public class RequestLogMiddleware : IMiddleware
    {
        private readonly ILogger<RequestLogMiddleware> _logger;
        private readonly IHostEnvironment _env;
        public RequestLogMiddleware(ILogger<RequestLogMiddleware> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var start = DateTimeOffset.UtcNow;
            try
            {
                // 繼續管道中的其他中介軟體
                await next(context);
            }
            catch(Exception ex)
            {
                _logger.LogError($"Error:{{@ExceptionInfo}}-{{@{nameof(LogTypeEnum)}}}", ex, LogTypeEnum.Error);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                if (_env.IsDevelopment() || _env.IsStaging())
                {
                    ExceptionDispatchInfo.Capture(ex).Throw(); //// 重新拋出原始異常並保留堆棧跟踪，給框架處理錯誤回傳內容，就仍然可在Swagger上直接看到詳細錯誤內容
                }
                return; // 如果不重新拋出異常，則提前終止處理，不跑後續的其他Middleware
            }
            finally
            {
                // 記錄Request跟Response，即使發生了異常也會執行
                LogResponse(context, start);
            }
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
}
