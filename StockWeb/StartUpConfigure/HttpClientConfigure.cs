using StockWeb.ConstData;

namespace StockWeb.StartUpConfigure
{
    public static class HttpClientConfigurator
    {
        public static void HttpClientConfigure(this WebApplicationBuilder builder)
        {
            var config = builder.Configuration;
            builder.Services.AddHttpClient();

            builder.Services.AddHttpClient(ConstHttpClinetName.TWSE, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.TWSE}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            });

            builder.Services.AddHttpClient(ConstHttpClinetName.openapiTwse, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.openapiTwse}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            });

            builder.Services.AddHttpClient(ConstHttpClinetName.TPEX, c =>
            {
                string baseAddress = config[$"{ConstHttpClinetName.TPEX}:{ConstString.BaseAddress}"]!;
                c.BaseAddress = new Uri(baseAddress);
            });
        }
    }
}
