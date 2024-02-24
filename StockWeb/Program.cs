
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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
            builder.Services.AddDbContext<StockContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("Stock"));
            });
            builder.Services.AddDbContextFactory<StockContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("Stock"));
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

            if(app.Environment.IsDevelopment())
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
    }
}
