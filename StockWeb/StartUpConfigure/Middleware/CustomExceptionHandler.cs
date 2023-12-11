using System.Runtime.ExceptionServices;

namespace StockWeb.StartUpConfigure.Middleware
{
    public class CustomExceptionHandler : IMiddleware
    {
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
            switch (exception)
            {
                case CustomErrorResponseException:
                    CustomErrorResponseException customException = (CustomErrorResponseException)exception;
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = customException.StatusCode;
                    var result = System.Text.Json.JsonSerializer.Serialize(new { error = customException.Message });
                    await context.Response.WriteAsync(result);

                    // 繼續執行後續中間件，例如將response寫入log的中間件，但是切記這邊已經寫入回傳訊息了，後續的中間件不能再改動response
                    break;
                default:
                    ExceptionDispatchInfo.Capture(exception).Throw(); //// 重新拋出原始異常並保留堆棧跟踪
                    break;
            }
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
