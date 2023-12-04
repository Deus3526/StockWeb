
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using StockWeb.DbModels;
using StockWeb.Enums;
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
            var builder = WebApplication.CreateBuilder(args);
            
            builder.SerilogConfigure();
            builder.Configuration.AddJsonFile("Stocks.json", optional: false, reloadOnChange: true);
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
            builder.Services.AddDbContext<StockContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("Stock"));
            });

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
           builder.SwaggerConfigure();
            builder.Services.AddSingleton<RequestLogMiddleware>();
            builder.Services.AddSingleton<CustomExceptionHandler>();
            builder.Services.AddScoped<StockService>();
            builder.Services.AddHttpClient();
            builder.Services.AddMemoryCache(options =>
            {
                // SizeLimit: 快取的大小限制，單位為字節。如果沒有設定，則快取的大小不受限制。
                options.SizeLimit = 1024 * 1024 * 100; // 例如，設定為 100 MB

                // CompactionPercentage: 當發生內存壓力時，快取將釋放的內存百分比。預設值通常為 0.2（即 20%）。
                options.CompactionPercentage = 0.2; // 可以設定為其他值

                // ExpirationScanFrequency: 快取清理過期項目的頻率。預設值通常為 1 分鐘。
                options.ExpirationScanFrequency = TimeSpan.FromMinutes(1); // 可以設定為其他時間間隔

                // TrackLinkedCacheEntries: 是否追蹤連結的快取項目以支援依賴性。例如移除掉A，依賴於A的B也會跟著移除。這個選項在預設情況下通常是 false。
                options.TrackLinkedCacheEntries = false; // 可以設定為 true，但會有額外的性能成本
            });
            //builder.Services.AddDistributedMemoryCache();  //如果之後要用Redis這種分布式緩存，可以先用這個頂著，即便預設也是在本地中儲存數據，但跟Redis是一樣的介面
            builder.Services.AddOutputCache(options =>
            {
                options.AddPolicy(nameof(OutputCacheWithAuthPolicy), policy => 
                {
                    policy.AddPolicy<OutputCacheWithAuthPolicy>()
                        .Expire(TimeSpan.FromMinutes(10)) // 设置为 10 分钟 ，不知道這個能不能寫進OutputCacheWithAuthPolicy裡面設定
                        .SetVaryByHeader(["Authorization"]); //不同的Header Authorization，要個別存快取，不知道這個能不能寫進OutputCacheWithAuthPolicy裡面設定
                });
            });
            var app = builder.Build();
            app.UseRequestLogMiddleware();
            app.UseCustomExceptionHandler();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UserMySwagger();
            }

            app.UseHttpsRedirection();

            
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseOutputCache();

            app.MapControllers();

            app.Run();
        }
    }
}
