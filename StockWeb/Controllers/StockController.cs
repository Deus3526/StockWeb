using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using StockWeb.DbModels;
using StockWeb.Models.ApiResponseModel;
using StockWeb.Services.ServicesForControllers;
using StockWeb.StartUpConfigure;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(ApiGroups.Stock))]
    [Tags(Tags.股票相關)]
    public class StockController : ControllerBase
    {
        private readonly StockContext _db;
        private readonly StockService _stockService;
        private readonly IMemoryCache _cache;
        public StockController(StockContext db,StockService stockService,IMemoryCache cache) 
        {
            _db= db;
            _stockService=stockService;
            _cache=cache;
        }

        [HttpGet]
        [OutputCache(PolicyName = nameof(OutputCacheWithAuthPolicy))]
        [Authorize]
        public async  Task<ActionResult<List<StockBaseInfo>>> test(int a)
        {
            var q =  await _db.StockBaseInfos.Take(50).ToListAsync();
            return Ok(q);
        }
        [HttpGet]
        public string CacheTest()
        {
            var q = _db.StockBaseInfos.First();
            StockBaseInfo? baseInfo = _cache.Get<StockBaseInfo>("test");
            if(baseInfo == null)
            {
                var cts = new CancellationTokenSource(); //創建一個CancellationTokenSource，以便可以從外部呼叫cts.Cancel()或cts.CancelAsync()來移除此快取
                var cacheEntryOptions = new MemoryCacheEntryOptions
                {
                    // 滑動過期時間，不設置即為永久，以設定5分鐘為例，如果這5分鐘都沒有調用到此項目，就會移除，即使過期時間設定一小時也是
                    SlidingExpiration = TimeSpan.FromMinutes(5),

                    // 絕對過期時間，不設置即為永久，可不設置，與相對的過期時間AbsoluteExpirationRelativeToNow擇一使用
                    //AbsoluteExpiration = DateTimeOffset.Now.AddHours(1),

                    // 相對於現在的絕對過期時間，不設置即為永久
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),

                    // 緩存優先級，預設是Normal，共有4個等級
                    //CacheItemPriority.Low：更有可能在内存压力下被移除。
                    //CacheItemPriority.Normal：默认值，平衡的驱逐概率。
                    //CacheItemPriority.High：不太可能在内存压力下被移除。
                    //CacheItemPriority.NeverRemove：尽可能不被移除，但在极端内存压力下仍可能
                    Priority = CacheItemPriority.Normal,

                    // 緩存大小，與註冊時的SizeLimit搭配，微軟不推薦使用
                    //Size = 1,

                    // 移除後的Callback
                    PostEvictionCallbacks = {
                        new PostEvictionCallbackRegistration
                        {
                            EvictionCallback = (key, value, reason, state) =>
                            {
                                // 在這裡添加驅逐後的邏輯
                                Console.WriteLine($"緩存項目 {key} 被驅逐，原因：{reason}");
                            }
                        }
                    },

                    // 過期令牌:添加一種或多種過期標記（expiration tokens），這些標記允許您根據外部變化來控制快取項目的有效性。當任何一個過期標記表示它代表的狀態已經改變時，與之關聯的快取項目將被自動移除。
                    //ExpirationTokens 的作用
                    //解耦與靈活性：使用 ExpirationTokens 可以在不直接觸及快取項目本身的情況下，從外部控制快取項目的生命週期。這提供了一種解耦的方式來管理快取，尤其在跨組件或跨系統工作時特別有用。
                    ExpirationTokens = {
                        new CancellationChangeToken(cts.Token)
                    }
                };
                _cache.Set<StockBaseInfo>("test",q);
                //cts.Cancel(); // 這將觸發快取項目過期
                return "No Cache,Add Now";
            }
            else
            {
                return "Has Cache";
            }
        }

        /// <summary>
        /// 更新DB上市櫃股票基本資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> UpdateStockBaseInfo()
        {
            await _stockService.UpdateStockBaseInfo();
            return Ok();
        }
    }
}
