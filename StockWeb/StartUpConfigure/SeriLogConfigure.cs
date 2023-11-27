using Serilog;
using Serilog.Filters;
using Serilog.Formatting.Compact;
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
            builder.Host.UseSerilog((context, services, configuration) => configuration
                    .ReadFrom.Configuration(context.Configuration)  //如果只有在appsetting設定的話，使用這個即可
                    .WriteTo.Logger(lc => lc //有用到filter的子logger，好像無法寫在appsetting.json裡面
                        .MinimumLevel.Information() //這個子Logger最低的level為Informaion，而且因為子Logger只能接收主Logger傳遞過來的資訊，所以子Logger的minLevel不能比主Logger高，否則會接收不到log訊息
                        .Filter.ByIncludingOnly(Matching.WithProperty(nameof(LogTypeEnum), LogTypeEnum.Request)) //有UserInformation這個property才寫進檔案
                        .WriteTo.File(new CompactJsonFormatter(), context.Configuration["LogPath:Request"]!, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180)) //使用json格式儲存log

                    .WriteTo.Logger(lc => lc //有用到filter的子logger，好像無法寫在appsetting.json裡面
                        .MinimumLevel.Information() //這個子Logger最低的level為Informaion，而且因為子Logger只能接收主Logger傳遞過來的資訊，所以子Logger的minLevel不能比主Logger高，否則會接收不到log訊息
                        .Filter.ByIncludingOnly(Matching.WithProperty(nameof(LogTypeEnum), LogTypeEnum.Login)) //有UserInformation這個property才寫進檔案
                        .WriteTo.File(new CompactJsonFormatter(), context.Configuration["LogPath:Login"]!, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180)) //使用json格式儲存log

                    .WriteTo.Logger(lc => lc //有用到filter的子logger，好像無法寫在appsetting.json裡面
                        .MinimumLevel.Information() //這個子Logger最低的level為Informaion，而且因為子Logger只能接收主Logger傳遞過來的資訊，所以子Logger的minLevel不能比主Logger高，否則會接收不到log訊息
                        .Filter.ByIncludingOnly(Matching.WithProperty(nameof(LogTypeEnum), LogTypeEnum.Error)) //有UserInformation這個property才寫進檔案
                        .WriteTo.File(new CompactJsonFormatter(), context.Configuration["LogPath:Error"]!, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 180)) //使用json格式儲存log
                );
        }
    }
}
