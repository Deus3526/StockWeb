using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Json;
using StockWeb.Enums;

namespace StockWeb.StartUpConfigure
{
    public static class SerilogConfigurator
    {
        /// <summary>
        /// 配置SeriLog的設定並使用
        /// </summary>
        /// <param name="builder"></param>
        public static void SerilogConfigure(this WebApplicationBuilder builder)
        {
            var formatter = new JsonFormatter( );
            builder.Host.UseSerilog((context, services, configuration) => {
                LogPath logPath = services.GetRequiredService<IOptions<LogPath>>().Value;
                configuration
                   .ReadFrom.Configuration(context.Configuration)  //如果只有在appsetting設定的話，使用這個即可
                   .WriteTo.Debug()

                   .WriteTo.Logger(lc => lc
                       .MinimumLevel.Information()
                       .Filter.ByExcluding(e => e.Properties["SourceContext"].ToString().Contains("Microsoft.EntityFrameworkCore"))
                       .WriteTo.Console())

                   .WriteTo.Logger(lc => lc
                       .MinimumLevel.Information()
                       .Filter.ByIncludingOnly(Matching.WithProperty(nameof(LogTypeEnum), LogTypeEnum.Request))
                       .WriteTo.File(formatter, logPath.Request, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180))

                   .WriteTo.Logger(lc => lc
                       .MinimumLevel.Information()
                       .Filter.ByIncludingOnly(Matching.WithProperty(nameof(LogTypeEnum), LogTypeEnum.Login))
                       .WriteTo.File(formatter, logPath.Login, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180))

                   .WriteTo.Logger(lc => lc
                       .MinimumLevel.Information()
                       .Filter.ByIncludingOnly(Matching.WithProperty(nameof(LogTypeEnum), LogTypeEnum.Error))
                       .WriteTo.File(formatter, logPath.Error, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180));
                }) ;
        }
    }
}
