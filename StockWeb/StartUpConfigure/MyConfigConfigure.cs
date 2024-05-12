using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using StockWeb.Services;
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
            builder.Services.AddOptions<JwtSettings>().BindConfiguration(nameof(JwtSettings),binder=>binder.ErrorOnUnknownConfiguration=true).ValidateDataAnnotations().ValidateOnStart();
            builder.Services.AddOptions<LogPath>().BindConfiguration(nameof(LogPath), binder => binder.ErrorOnUnknownConfiguration = true).ValidateDataAnnotations().ValidateOnStart();
            builder.Services.AddOptions<StockSource>().BindConfiguration(nameof(StockSource), binder => binder.ErrorOnUnknownConfiguration = true).ValidateDataAnnotations().ValidateOnStart();
            builder.Services.AddOptions<ConnectionStrings>().BindConfiguration(nameof(ConnectionStrings), binder => binder.ErrorOnUnknownConfiguration = true).ValidateDataAnnotations().ValidateOnStart();
        }
    }

    public record JwtSettings
    {
        [Required]
        public required string Key { get; init; }
        [Required]
        public required string Issuer { get; init; }
        [Required]
        public int ExpiredTime { get; init; }
        [Required]
        public int RefreshTokenExpiredTime { get; init; }
    }

    public record LogPath
    {
        [Required]
        public required string Request { get; init; }
        [Required]
        public required string Login { get; init; }
        [Required]
        public required string Error { get; init; }
    }

    public record StockSource
    {
        [Required, ValidateObjectMembers]
        public required TwseContent Twse { get; init; }
        [Required,ValidateObjectMembers]
        public required OpenapiTwseContent OpenapiTwse { get; init; }
        [Required,ValidateObjectMembers]
        public required TpexContent Tpex { get; init; }


        #region 內部模型

        public record TwseContent
        {
            [Required]
            public required string BaseAddress { get; init; }
            [Required, ValidateObjectMembers]
            public required RouteContent Route { get; init; }

            public record RouteContent
            {
                [Required]
                public required string 上市股票盤後基本資訊 { get; init; }
                [Required]
                public required string 上市股票盤後當沖資訊 { get; init; }
                [Required]
                public required string 上市股票盤後融資融券資訊 { get; init; }
                [Required]
                public required string 上市股票盤後借券資訊 { get; init; }
                [Required]
                public required string 上市股票盤後外資資訊 { get; init; }
                [Required]
                public required string 上市股票盤後投信資訊 { get; init; }
                [Required]
                public required string 上市大盤成交資訊 { get; init; }

                [Required]
                public required string 上市股票殖利率資訊 { get; set; }
            }
        }
        public record OpenapiTwseContent
        {
            [Required]
            public required string BaseAddress { get; init; }

            [Required, ValidateObjectMembers]
            public required RouteContent Route { get; init; }

            public record RouteContent
            {
                [Required]
                public required string 上市股票基本訊息_計算流通張數 { get; set; }
            }

        }
        public record TpexContent
        {
            [Required]
            public required string BaseAddress { get; init; }

            [Required,ValidateObjectMembers]
            public required RouteContent Route { get; init; }
            public record RouteContent
            {
                [Required]
                public required string 上櫃股票盤後基本資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後當沖資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後融資融券資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後借券資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後外資淨買超資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後外資淨賣超資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後投信淨買超資訊 { get; init; }
                [Required]
                public required string 上櫃股票盤後投信淨賣超資訊 { get; init; }
                [Required]
                public required string 上櫃股票基本訊息_發行股數 { get; init; }
            }
        }

 

        #endregion
    }

    public record ConnectionStrings
    {
        [Required]
        public required string Stock { get; init; }
    }





}
