using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using StockWeb.DbModels;
using StockWeb.Services;
using StockWeb.Services.ServicesForControllers;
using StockWeb.StartUpConfigure;
using StockWeb.StartUpConfigure.Middleware;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var systemLogger = CreateSystemLogger();
            systemLogger.Information("程式開始");
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.SerilogConfigure();
                builder.MyConfigConfigure();
                // Add services to the container.
                builder.Services.AddControllers(options =>
                {
                    //options.Filters.Add(new AuthorizeFilter());
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });

                builder.JwtConfigure();
                builder.Services.AddDbContext<StockContext>((serviceProvider, options) =>
                {
                    //options.UseSqlServer(builder.Configuration.GetConnectionString("Stock"));
                    var connectionStrings = serviceProvider.GetRequiredService<IOptions<ConnectionStrings>>().Value;
                    options.UseSqlServer(connectionStrings.Stock, sqlOptions => sqlOptions.CommandTimeout(60));
                });
                // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddCors();
                builder.SwaggerConfigure();
                builder.HttpClientConfigure();
                builder.Services.AddScoped<RequestApiService>();
                builder.AspectCoreConfigure();
                builder.Services.AddSingleton<RequestLogMiddleware>();
                builder.Services.AddSingleton<CustomExceptionHandler>();
                builder.Services.AddScoped<StockService>();
                builder.Services.AddSingleton<EventBus>();
                builder.Services.AddHostedService<StockBreakout60MaService>();
                builder.Services.AddSingleton<StockBreakout60MaService>();
                builder.Services.AddMemoryCache();
                builder.Services.AddOutputCache();
                //builder.Services.AddDistributedMemoryCache();  //如果之後要用Redis這種分布式緩存，可以先用這個頂著，即便預設也是在本地中儲存數據，但跟Redis是一樣的介面
                var app = builder.Build();
                app.UseRequestLogMiddleware();
                app.UseCustomExceptionHandler();
                // Configure the HTTP request pipeline.
                //if (app.Environment.IsDevelopment())
                //{
                //    app.UserMySwagger();
                //}
                app.UserMySwagger();
                app.UseHttpsRedirection();

                if (app.Environment.IsDevelopment())
                {
                    app.UseCors(builder =>
                    {
                        builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                    });
                }

                app.UseAuthentication();
                app.UseAuthorization();
                app.UseOutputCache();

                app.MapControllers();
                app.Run();
            }
            catch (Exception ex)
            {
                systemLogger.Fatal("發生錯誤");
                systemLogger.Fatal($"Error: {@ex}", ex);
            }
            finally
            {
                Log.CloseAndFlush();
                systemLogger.Information("程式關閉");
            }

        }

        /// <summary>
        /// 在主要log建立之前，先配置的一個簡單系統log，用於記錄系統的開啟關閉事件。
        /// 如果使用網路上的Log.Logger寫法而不是var systemLogger，則全局的Log.Logger會在UseSeriLog的時候被替換掉，故這邊使用局部的logger
        /// </summary>
        /// <returns></returns>
        private static Serilog.Core.Logger CreateSystemLogger()
        {
            var systemLogger = new LoggerConfiguration()
               .MinimumLevel.Information()
               .WriteTo.Console()
               .WriteTo.File("../logs/System/system-.json",  //雖然可以用ConfigurationBuilder先Build起來拿到配置文件，但是仍然為弱型別的使用方式，先寫死看後續有沒有更好寫法
                   rollingInterval: RollingInterval.Hour, // 每小時一個檔案
                   retainedFileCountLimit: 24 * 30 // 最多保留 30 天份的 Log 檔案
               )
               .CreateLogger();
            return systemLogger;
        }
    }
}
