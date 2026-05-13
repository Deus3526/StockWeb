using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using NewStock.Middleware;
using NLog;
using NLog.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NewStock
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders();
            builder.Host.UseNLog();

            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            });
            builder.Services.AddOpenApi();
            builder.Services.AddSwaggerUI();

            var app = builder.Build();
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("程式執行成功");
            app.Services.GetRequiredService<IHostApplicationLifetime>()
                .ApplicationStopped.Register(LogManager.Shutdown);

            app.MapOpenApi();
            app.MapSwaggerUI();

            app.UseHttpsRedirection();

            app.UseExceptionHandling();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
