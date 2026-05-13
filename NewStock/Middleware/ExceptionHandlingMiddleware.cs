using Microsoft.Extensions.Hosting;

namespace NewStock.Middleware
{
    public sealed class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "未處理例外");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json; charset=utf-8";

                var payload = new
                {
                    traceId = context.TraceIdentifier,
                    message = env.IsDevelopment() ? ex.Message : "伺服器發生錯誤。"
                };

                await context.Response.WriteAsJsonAsync(payload).ConfigureAwait(false);
            }
        }
    }

    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static WebApplication UseExceptionHandling(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            return app;
        }
    }
}