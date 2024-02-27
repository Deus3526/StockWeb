using AspectCore.DynamicProxy;
using AspectCore.Extensions.DependencyInjection;
using StockWeb.Enums;
using StockWeb.StartUpConfigure.Middleware;
using System.Runtime.ExceptionServices;

namespace StockWeb.StartUpConfigure
{
    public static class AspectCoreConfigurator
    {
        public static void AspectCoreConfigure(this WebApplicationBuilder builder)
        {
            builder.Host.UseServiceProviderFactory(new DynamicProxyServiceProviderFactory());
            builder.Services.ConfigureDynamicProxy();
        }
    }

    public class LoggingInterceptorAttribute : AbstractInterceptorAttribute
    {
        /// <summary>
        /// 執行方法的名字
        /// </summary>
        public string? MethodName { get; set; } = null;
        /// <summary>
        /// 發生錯誤時的錯誤訊息
        /// </summary>
        public string? ErrorMessage { get; set; } = null;
        /// <summary>
        /// 發生錯誤時，在正式環境丟CustomErrorResponseException時，要回傳的狀態碼
        /// </summary>
        public int StatusCode { get; set; } = StatusCodes.Status500InternalServerError;
       
        public LoggingInterceptorAttribute() { }
        public override async Task Invoke(AspectContext context, AspectDelegate next)
        {
            IWebHostEnvironment _env=context.ServiceProvider.GetService<IWebHostEnvironment>()!;
            //ILogger<LoggingInterceptorAttribute> _logger = context.ServiceProvider.GetService<ILogger<LoggingInterceptorAttribute>>()!;
            var loggerType = typeof(ILogger<>).MakeGenericType(context.Implementation.GetType().BaseType!);
            var _logger = (context.ServiceProvider.GetService(loggerType) as ILogger)!;


            MethodName ??= context.ImplementationMethod.Name;
            ErrorMessage ??= $"{MethodName} : 發生錯誤";
            try
            {
                _logger.LogInformation("{MethodName} : 開始",MethodName);
                await next(context); // 调用原始方法
                _logger.LogInformation("{MethodName} : 結束",MethodName);
            }

            catch (Exception ex)
            {
                _logger.LogError($"Error:{{@ErrorMessage}}-{{@ExceptionInfo}}-{{@{nameof(LogTypeEnum)}}}", ErrorMessage,ex, LogTypeEnum.Error);
                throw new CustomErrorResponseException(ErrorMessage, StatusCode); 
            }
        }
    }
}
