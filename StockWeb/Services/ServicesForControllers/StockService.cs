using StockWeb.DbModels;
using StockWeb.Models.ApiResponseModel;
using StockWeb.StartUpConfigure.Middleware;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using System.Collections.Concurrent;
using StockWeb.Enums;

namespace StockWeb.Services.ServicesForControllers
{
    public class StockService
    {
        private readonly StockContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DateTime _date=DateTime.Now.Date;
        public StockService(StockContext db, IConfiguration config,IHttpClientFactory httpClientFactory)
        {
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
        }


        #region 更新股票基本資訊表
        public async Task UpdateStockBaseInfo()
        {
            List<StockBaseInfo> stockBaseInfos = _db.StockBaseInfos.ToList();
            ConcurrentBag<StockBaseInfo> bags = new ConcurrentBag<StockBaseInfo>();
            List<Task> tasks = new List<Task>();
            tasks.Add(取得上市股票基本訊息_計算流通張數(bags));
            tasks.Add(取得上櫃股票基本訊息_發行股數(bags));

            await Task.WhenAll(tasks);
            foreach (var bag in bags)
            {
                StockBaseInfo? baseInfo = stockBaseInfos.FirstOrDefault(s => s.StockId == bag.StockId);
                if (baseInfo == null)
                {
                    _db.StockBaseInfos.Add(bag);
                }
                else
                {
                    baseInfo.UpdateStockBaseInfo(bag);
                }
            }
            await _db.SaveChangesAsync();
            return;
        }

        private async Task 取得上市股票基本訊息_計算流通張數(ConcurrentBag<StockBaseInfo> bags)
        {
            var url = _config["上市股票基本訊息_計算流通張數"];
            HttpClient client = _httpClientFactory.CreateClient();
            List<上市股票基本資訊>? res = null;
            try
            {
                res = await client.GetFromJsonAsync<List<上市股票基本資訊>>(url);
            }
            catch
            {
                throw new CustomErrorResponseException("取得上市股票基本訊息時發生錯誤", StatusCodes.Status502BadGateway);
            }
            ArgumentNullException.ThrowIfNull(res);
            foreach (上市股票基本資訊 s in res)
            {
                ArgumentNullException.ThrowIfNull(s.公司代號);
                int stockId = s.公司代號.Value;
                StockBaseInfo? baseInfo = bags.FirstOrDefault(b => b.StockId == stockId);
                if (baseInfo == null)
                {
                    baseInfo = new StockBaseInfo();
                    baseInfo.StockId = stockId;
                    baseInfo.StockType = StockTypeEnum.tse;
                    bags.Add(baseInfo);
                }
                ArgumentNullException.ThrowIfNull(s.公司簡稱);
                baseInfo.StockName = s.公司簡稱.Trim();
                ArgumentNullException.ThrowIfNull(s.已發行普通股數或TDR原股發行股數);
                decimal 發行股數 = s.已發行普通股數或TDR原股發行股數.Value;
                int 發行張數 = (int)(發行股數 / 1000);
                baseInfo.StockAmount = 發行張數;
            }
        }

        private async Task 取得上櫃股票基本訊息_發行股數(ConcurrentBag<StockBaseInfo> bags)
        {
            var url = _config["上櫃股票基本訊息_發行股數"];
            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                var res = await client.GetFromJsonAsync<上櫃股票基本資訊_發行股數回傳結果>(url);
                ArgumentNullException.ThrowIfNull(res);
                res.轉換為上櫃股票基本資訊_流通股數(bags);
                res = null;

                return;
            }
            catch
            {
                throw new CustomErrorResponseException("取得上市股票基本訊息時發生錯誤", StatusCodes.Status502BadGateway);
            }
        }
        #endregion


        /// <summary>
        /// 返回 20230526的格式
        /// </summary>
        string dateString
        {
            get
            {
                return _date.ToString("yyyyMMdd");
            }
        }
        /// <summary>
        /// 返回111/05/26的格式
        /// </summary>
        string dateTaiwan
        {
            get
            {
                CultureInfo culture = new CultureInfo("zh-TW");
                culture.DateTimeFormat.Calendar = new TaiwanCalendar();
                return _date.ToString("yyy/MM/dd", culture);
            }
        }
    }
}
