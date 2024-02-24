using Microsoft.OpenApi.Models;

namespace StockWeb.StartUpConfigure
{
    public static class MyConfigConfigurator
    {
        public static void MyConfigConfigure(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("Stocks.json", optional: false, reloadOnChange: true);
        }
    }

    public record MyConfig
    {

    }

}
