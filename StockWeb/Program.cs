
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
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
using StockWeb.StartUpConfigure;
using StockWeb.StartUpConfigure.Middleware;
using StockWeb.StaticData;
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
            // Add services to the container.
            builder.SerilogConfigure();
            builder.Services.AddControllers(options => 
            {
                options.Filters.Add(new AuthorizeFilter());
            }).AddJsonOptions(options => 
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
            builder.Services.AddSwaggerGen(options =>
            {
                foreach (var field in typeof(ApiGroups).GetFields())
                {
                    var group = (ApiGroup)field.GetValue(null)!;
                    options.SwaggerDoc(field.Name, new OpenApiInfo
                    {
                        Title = group.DownListName,
                        Description = group.Description,
                        Version = group.Version
                    });
                }
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "SwaggerApi.xml"));
                options.AddSecurityDefinition("Jwt_Login", new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "請輸入你的token"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Jwt_Login" //與上面的SecurityDefinition的第一個參數要一致
                            }
                        },
                        new string[] {}
                    }
                });
            });
            builder.Services.AddSingleton<RequestLogMiddleware>();
            var app = builder.Build();
            app.UseMiddleware<RequestLogMiddleware>();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    foreach (var field in typeof(ApiGroups).GetFields())
                    {
                        var group = (ApiGroup)field.GetValue(null)!;
                        options.SwaggerEndpoint($"/swagger/{field.Name}/swagger.json", group.DownListName);
                    }
                });
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
