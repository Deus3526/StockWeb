using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace StockWeb.StartUpConfigure
{
    public static class MyConfigConfigurator
    {
        public static void MyConfigConfigure(this WebApplicationBuilder builder)
        {
            builder.Configuration.AddJsonFile("Stocks.json", optional: false, reloadOnChange: true);
            //builder.Services.AddOptions<MyConfig>().BindConfiguration("", configureBinder =>
            //{
            //    configureBinder.ErrorOnUnknownConfiguration = true; //這個選項改成true的話，框架會檢查ConfigSectionPath所擁有的所有資料是否都有對應到模型中，沒有的話會拋出Exception
            //});
            //使用ValidateDataAnnotations來按照DataAnnotations的設定來驗證屬性
            //使用ValidateOnStart可在app.Run啟動階段便驗證，不然要等到實際注入的時候才會做驗證動作，但是使用ValidateOnStart的話Logger要配合能夠在啟動階段寫下log，否則驗證失敗可能會找不到系統啟動失敗的原因
            builder.Services.AddOptions<ConnectionStrings>().BindConfiguration(nameof(ConnectionStrings), binder => binder.ErrorOnUnknownConfiguration = true).ValidateDataAnnotations().ValidateOnStart();
        }
    }

    public record ConnectionStrings
    {
        [Required]
        public required string Stock { get; init; }
    }





}
