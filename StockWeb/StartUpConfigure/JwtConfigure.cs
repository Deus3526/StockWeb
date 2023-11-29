using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using StockWeb.Services;
using System.Text;

namespace StockWeb.StartUpConfigure
{
    public static class JwtConfigurator
    {
        /// <summary>
        /// 配置jwt
        /// </summary>
        /// <param name="builder"></param>
        public static void JwtConfigure(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<JwtSettings>(ServiceProvider =>
            {
                JwtSettings jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()!;
                return jwtSettings;
            });
            //這邊使用AddOptions來設定JwtBearerOptions，簡易版的可以直接在AddJwtBearer()裡面設定，但是因為我這邊想要注入前面註冊的JwtSettings，所以要使用AddOptions的寫法，才能拿到JwtSettings
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<JwtSettings>((options, jwtSettings) =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        // 你的其它验证参数…
                        ValidateLifetime = true, // 確保你開啟了生命週期驗證 ，預設是true，才會檢查是否過期
                        ClockSkew = TimeSpan.Zero, //過期的緩衝時間，預設是5分鐘，意即過期時間設定10分鐘，則15分鐘後才會真正過期
                        ValidateIssuer = true, //驗證Jwt的發行者
                        ValidateIssuerSigningKey = true,//驗證Jwt的密鑰是否有效
                        ValidateAudience = false,//是否驗證受眾，如果有需要用到再查怎麼設定
                        ValidIssuer = jwtSettings.Issuer, //指定發行者
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key))  //採用對稱加密
                    };
                });
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<JwtService>();
        }
    }
}
