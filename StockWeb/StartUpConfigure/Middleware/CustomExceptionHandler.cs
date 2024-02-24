using StockWeb.Enums;
using System.Runtime.ExceptionServices;

namespace StockWeb.StartUpConfigure.Middleware
{
    public class CustomExceptionHandler(ILogger<RequestLogMiddleware> logger, IHostEnvironment env) : IMiddleware
    {
        private readonly ILogger<RequestLogMiddleware> _logger = logger;
        private readonly IHostEnvironment _env = env;

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, next, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, RequestDelegate next, Exception exception)
        {
            context.Response.ContentType = "application/json";
            string result = string.Empty;
            switch (exception)
            {
                case CustomErrorResponseException customException:
                    context.Response.StatusCode = customException.StatusCode;
                    result = System.Text.Json.JsonSerializer.Serialize(new { error = customException.Message });
                    break;

                default:
                    _logger.LogError($"Error:{{@ExceptionInfo}}-{{@{nameof(LogTypeEnum)}}}", exception, LogTypeEnum.Error);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    result = System.Text.Json.JsonSerializer.Serialize(new { error = "發生未知的錯誤" });
                    break;
            }
            await context.Response.WriteAsync(result);// 繼續執行後續中間件，例如將response寫入log的中間件，但是切記這邊已經寫入回傳訊息了，後續的中間件不能再改動response
        }
    }
    public static class CustomExceptionHandlerExtension
    {
        /// <summary>
        /// 對於自己拋出的CustomErrorResponseException，使用Middleware統一處理
        /// </summary>
        /// <param name="app"></param>
        public static void UseCustomExceptionHandler(this IApplicationBuilder app)
        {
            app.UseMiddleware<CustomExceptionHandler>();
        }
    }
    /// <summary>
    /// 這個自訂的Exception，用於給自訂的Middleware:CustomExceptionHandler捕捉，會按照訊息及狀態回傳Response
    /// </summary>
    public class CustomErrorResponseException : Exception
    {
        // 額外的屬性，用於存儲狀態碼
        public int StatusCode { get; }
        // 構造函數：只有訊息，默認狀態碼為 500
        public CustomErrorResponseException(string message)
            : base(message)
        {
            StatusCode = 500; // 默認狀態碼為 500
        }
        /// <summary>
        /// 這個自訂的Exception，用於給自訂的Middleware:CustomExceptionHandler捕捉，會按照訊息及狀態回傳Response
        /// </summary>
        /// <param name="message"></param>
        /// <param name="statusCode"></param>
        // 構造函數：訊息和狀態碼
        public CustomErrorResponseException(string message, int statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
        // 構造函數：訊息、狀態碼和內部異常
        public CustomErrorResponseException(string message, int statusCode, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }
}
