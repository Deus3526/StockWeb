using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace StockWeb.StartUpConfigure
{
    public static class SwaggerConfigurator
    {

        /// <summary>
        /// 註冊及設定Swagger文件
        /// </summary>
        /// <param name="builder"></param>
        public static void SwaggerConfigure(this WebApplicationBuilder builder)
        {
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
                        Array.Empty<string>()
                    }
                });
                options.DocumentFilter<TagOrderDocumentFilter>();
            });
        }


        /// <summary>
        /// 設定Swagger Middleware及UI
        /// </summary>
        /// <param name="app"></param>
        public static void UserMySwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                foreach (var field in typeof(ApiGroups).GetFields())
                {
                    var group = (ApiGroup)field.GetValue(null)!;
                    options.SwaggerEndpoint($"/swagger/{field.Name}/swagger.json", group.DownListName);
                }
                options.DefaultModelsExpandDepth(-1); //在最下方不顯示Schemas的說明
            });
        }
    }

    public static class ApiGroups
    {
        public static readonly ApiGroup Stock = new("股票WebApi", "股票資訊從櫃買中心、證交所、公開資訊觀測站取得", "v1");
        public static readonly ApiGroup Test = new("測試用", "各種機制測試", "v1");

    }
    public class ApiGroup(string downListName, string description, string version)
    {
        public string DownListName { get;} = downListName;
        public string Description { get; } = description;
        public string Version { get; } = version;
    }

    public static class Tags
    {
        public const string 登入相關 = "登入相關";
        public const string 股票相關 = "股票相關";
    }
    public class TagOrderDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // 標籤的優先排序
            var tagOrder = new List<string> 
            { 
                Tags.登入相關, 
                Tags.股票相關 
            };

            // 根据标签排序
            var orderedPaths = swaggerDoc.Paths
                .OrderBy(path => GetOrderForPath(path.Value.Operations, tagOrder, context))
                .ToDictionary(path => path.Key, path => path.Value);

            // 更新 Swagger 文档的路径
            swaggerDoc.Paths = [];
            foreach (var path in orderedPaths)
            {
                swaggerDoc.Paths.Add(path.Key, path.Value);
            }
        }

        private int GetOrderForPath(IDictionary<OperationType, OpenApiOperation> operations, List<string> tagOrder, DocumentFilterContext context)
        {
            var minOrder = int.MaxValue;

            foreach (var operation in operations.Values)
            {
                foreach (var tag in operation.Tags)
                {
                    var index = tagOrder.IndexOf(tag.Name);
                    if (index >= 0 && index < minOrder)
                    {
                        minOrder = index;
                    }
                }
            }

            return minOrder == int.MaxValue ? tagOrder.Count : minOrder;
        }
    }
}
