using StockWeb.DbModels;
using StockWeb.Models.ApiResponseModel;
using StockWeb.StartUpConfigure.Middleware;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Globalization;
using System.Collections.Concurrent;
using StockWeb.Enums;
using Microsoft.EntityFrameworkCore;
using StockWeb.Extensions;
using StockWeb.StartUpConfigure;

namespace StockWeb.Services.ServicesForControllers
{
    public class StockService
    {
        private readonly StockContext _db;
        private readonly IConfiguration _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<StockService> _logger;
        public StockService(StockContext db, IConfiguration config, IHttpClientFactory httpClientFactory, ILogger<StockService> logger)
        {
            _db = db;
            _config = config;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }


        #region 更新股票基本資訊表
        /// <summary>
        /// 更新股票基本資訊表
        /// </summary>
        /// <returns></returns>
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


        #region 更新日成交資訊
        /// <summary>
        /// 依據參數isHistoricalUpdate往前或往後更新日成交資訊，如果Db沒有資料，插入2021/1/1的資料
        /// </summary>
        /// <param name="isHistoricalUpdate"></param>
        /// <returns></returns>
        public async Task UpdateStockDayInfo(bool isHistoricalUpdate)
        {
            DateOnly? date = isHistoricalUpdate ?
                (await _db.StockDayInfos.Select(s => (DateOnly?)s.Date).DefaultIfEmpty().MinAsync())?.AddDays(-1) :
                (await _db.StockDayInfos.Select(s => (DateOnly?)s.Date).DefaultIfEmpty().MaxAsync())?.AddDays(1);

            if (date == null) date = new DateOnly(2021, 1, 4);  //如果Db完全沒有日成交資料
            await UpdateStockDayInfoByDate(date.Value);
        }

        /// <summary>
        /// 更新指定日期的日成交資訊
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task UpdateStockDayInfoByDate(DateOnly date)
        {
            ConcurrentBag<StockDayInfo> dayInfoBags = _db.StockBaseInfos.AsNoTracking().Select(s => new StockDayInfo { StockId = s.StockId, Date = date }).ToConcurrentBag();
            List<StockBaseInfo> baseInfos = await _db.StockBaseInfos.AsNoTracking().ToListAsync();
            List<Task> tasks = new List<Task>();
            tasks.Add(DeleteDayInfoRange(date, date));
            tasks.Add(UpdateTseDayInfo(dayInfoBags, date, baseInfos));
            tasks.Add(UpdateOtcDayInfo(dayInfoBags, date, baseInfos));
            await Task.WhenAll(tasks);
            return;
        }
        /// <summary>
        /// 刪除指定期間內的日成交資料
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private async Task DeleteDayInfoRange(DateOnly? startDate, DateOnly? endDate)
        {
            await _db.StockDayInfos.Where(s =>
                 (startDate == null ? true : s.Date >= startDate) &&
                 (endDate == null ? true : s.Date <= endDate)
             ).ExecuteDeleteAsync();
            return;
        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task UpdateTseDayInfo(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date, List<StockBaseInfo> baseInfos)
        {
            List<Task> tasks = new List<Task>();
            tasks.Add( 更新上市股票盤後基本資訊(dayInfoBags, date, baseInfos));
            tasks.Add(更新上市股票盤後當沖資訊(dayInfoBags, date));
            tasks.Add(更新上市股票盤後融資融券資訊(dayInfoBags, date));
            tasks.Add(更新上市股票盤後借券資訊(dayInfoBags, date));
            //tasks.Add(updateTseDayInfoForeign(dayInfoBags));
            //tasks.Add(updateTseDayInfoInvest(dayInfoBags));

            await Task.WhenAll(tasks);
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        private async Task UpdateOtcDayInfo(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date, List<StockBaseInfo> baseInfos)
        {
            List<Task> tasks = new List<Task>();
            //tasks.Add(updateOtcDayInfoBase(dayInfos));
            //tasks.Add(updateOtcDayInfoMargin(dayInfos));
            //tasks.Add(updateOtcDayInfoLend(dayInfos));
            //tasks.Add(updateOtcDayInfoDayTrade(dayInfos));
            //tasks.Add(updateOtcDayInfoInvest(dayInfos));
            //tasks.Add(updateOtcDayInfoForeign(dayInfos));
            await Task.WhenAll(tasks);
        }


        [LoggingInterceptor(StatusCode =StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後基本資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date,List<StockBaseInfo> baseInfos)
        {
            string url = _config["上市股票盤後基本資訊"] + date.ToDateFormateForTse();
            HttpClient client = _httpClientFactory.CreateClient();
            var res=await client.GetFromJsonAsync<上市股票盤後基本資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.tables[8].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                StockBaseInfo baseInfo = baseInfos.First(b=>b.StockId==stockId);
                dayInfo.成交量 = Convert.ToInt32(decimal.Parse(row[2])) / 1000;
                dayInfo.開盤價 = double.Parse(row[5]);
                dayInfo.最高價 = double.Parse(row[6]);
                dayInfo.最低價 = double.Parse(row[7]);
                dayInfo.收盤價 = double.Parse(row[8]);
                int direction = row[9].Contains("+") ? 1 : -1;
                double 漲跌價差 = double.Parse(row[10]) * direction;
                dayInfo.平盤價 = dayInfo.收盤價 - 漲跌價差;
                dayInfo.漲幅 = 漲跌價差 / dayInfo.平盤價;
                dayInfo.本益比 = double.Parse(row[15]);
                dayInfo.周轉率 = dayInfo.成交量 / baseInfo.StockAmount;
            }
            return;

        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後當沖資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上市股票盤後當沖資訊"] + date.ToDateFormateForTse();
            HttpClient client = _httpClientFactory.CreateClient();
            var res = await client.GetFromJsonAsync<上市股票盤後當沖資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.tables[1].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach(var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.當沖成交張數 = Convert.ToInt32(decimal.Parse(row[3]) / 1000);
            }
            return;
        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後融資融券資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上市股票盤後融資融券資訊"] + date.ToDateFormateForTse();
            HttpClient client = _httpClientFactory.CreateClient();
            var res = await client.GetFromJsonAsync<上市股票盤後融資融券資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.tables[1].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach(var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.融資買入 = Convert.ToInt32(decimal.Parse(row[2]));
                dayInfo.融資賣出= Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.融資買賣超 = dayInfo.融資買入 - dayInfo.融資賣出;
                dayInfo.融資餘額= Convert.ToInt32(decimal.Parse(row[6]));
                dayInfo.融券買入= Convert.ToInt32(decimal.Parse(row[8]));
                dayInfo.融券賣出= Convert.ToInt32(decimal.Parse(row[9]));
                dayInfo.融券買賣超 = dayInfo.融券買入 - dayInfo.融券賣出;
                dayInfo.融券餘額= Convert.ToInt32(decimal.Parse(row[12]));
            }
            return;
        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後借券資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            throw new NotImplementedException();
            string url = _config["上市股票盤後借券資訊"] + date.ToDateFormateForTse();
            HttpClient client = _httpClientFactory.CreateClient();
            上市股票盤後借券資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上市股票盤後借券資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach(var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.借券買入 = Convert.ToInt32(decimal.Parse(row[10]) / 1000);
                dayInfo.借券賣出= Convert.ToInt32(decimal.Parse(row[9]) / 1000);
                dayInfo.借券買賣超 = dayInfo.借券買入 - dayInfo.借券賣出;
                dayInfo.借券餘額= Convert.ToInt32(decimal.Parse(row[12]) / 1000);
            }
            return;
        }

        //[LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        //protected virtual async Task 更新上市股票盤後外資資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        //{
        //    return;
        //}
        #endregion
    }
}
