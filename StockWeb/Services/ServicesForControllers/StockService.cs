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
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        public virtual async Task UpdateStockBaseInfo()
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

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 取得上市股票基本訊息_計算流通張數(ConcurrentBag<StockBaseInfo> bags)
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

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 取得上櫃股票基本訊息_發行股數(ConcurrentBag<StockBaseInfo> bags)
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
        /// 依據參數isHistoricalUpdate往前或往後更新日成交資訊，如果Db沒有資料，插入2021/1/4的資料
        /// </summary>
        /// <param name="isHistoricalUpdate"></param>
        /// <returns></returns>
        public async Task UpdateStockDayInfo(bool isHistoricalUpdate)
        {
            DateOnly? date = isHistoricalUpdate ?
                (await _db.StockDayInfos.Select(s => (DateOnly?)s.Date).DefaultIfEmpty().MinAsync())?.AddDays(-1) :
                (await _db.StockDayInfos.Select(s => (DateOnly?)s.Date).DefaultIfEmpty().MaxAsync())?.AddDays(1);

            if (date == null) date = new DateOnly(2023, 1, 4);  //如果Db完全沒有日成交資料，從2021/01/04開始計算
            await UpdateStockDayInfoByDate(date.Value);
        }

        /// <summary>
        /// 更新指定日期的日成交資訊
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public virtual async Task UpdateStockDayInfoByDate(DateOnly date)
        {
            ConcurrentBag<StockDayInfo> dayInfoBags = _db.StockBaseInfos.AsNoTracking().Select(s => new StockDayInfo { StockId = s.StockId, Date = date }).ToConcurrentBag();
            List<StockBaseInfo> baseInfos = await _db.StockBaseInfos.AsNoTracking().ToListAsync();
            List<Task> tasks = new List<Task>();
            tasks.Add(DeleteDayInfoRange(date, date));
            tasks.Add(UpdateTseDayInfo(dayInfoBags, date, baseInfos));
            tasks.Add(UpdateOtcDayInfo(dayInfoBags, date, baseInfos));
            await Task.WhenAll(tasks);

            await _db.StockDayInfos.AddRangeAsync(dayInfoBags);
            _db.SaveChanges();
            return;
        }
        /// <summary>
        /// 刪除指定期間內的日成交資料
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task DeleteDayInfoRange(DateOnly? startDate, DateOnly? endDate)
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

            //當沖率計算需要基本資訊的成交量，所以要確保在其之後執行
            tasks.Add(
                更新上市股票盤後基本資訊(dayInfoBags, date, baseInfos).ContinueWith(async t =>
                        {
                            if(t.IsCompletedSuccessfully) await 更新上市股票盤後當沖資訊(dayInfoBags, date);
                        }).Unwrap() //這邊的ContinueWith會回傳Task<Task>，需要用Unwrap轉換為Task才能加進tasks
            ) ; 

            tasks.Add(更新上市股票盤後融資融券資訊(dayInfoBags, date));
            tasks.Add(更新上市股票盤後借券資訊(dayInfoBags, date));
            tasks.Add(更新上市股票盤後外資資訊(dayInfoBags, date));
            tasks.Add(更新上市股票盤後投信資訊(dayInfoBags, date));

            await Task.WhenAll(tasks);
           
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task UpdateOtcDayInfo(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date, List<StockBaseInfo> baseInfos)
        {
            List<Task> tasks = new List<Task>();
            //當沖率計算需要基本資訊的成交量，所以要確保在其之後執行
            tasks.Add(
                更新上櫃股票盤後基本資訊(dayInfoBags, date, baseInfos).ContinueWith(async t =>
                {
                    if (t.IsCompletedSuccessfully) await 更新上櫃股票盤後當沖資訊(dayInfoBags, date);
                }).Unwrap() //這邊的ContinueWith會回傳Task<Task>，需要用Unwrap轉換為Task才能加進tasks
            );
            tasks.Add(更新上櫃股票盤後融資融券資訊(dayInfoBags,date));
            tasks.Add(更新上櫃股票盤後借券資訊(dayInfoBags,date));
            tasks.Add(更新上櫃股票盤後外資淨買超資訊(dayInfoBags,date));
            tasks.Add(更新上櫃股票盤後外資淨賣超資訊(dayInfoBags, date));
            tasks.Add(更新上櫃股票盤後投信淨買超資訊(dayInfoBags, date));
            tasks.Add(更新上櫃股票盤後投信淨賣超資訊(dayInfoBags, date));
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
                bool 是否有成交價 = double.TryParse(row[5], out double temp開盤價);
                if(!是否有成交價) temp開盤價 = double.Parse(row[11]);  //如果沒有成交價格，使用最後揭示買價
                dayInfo.開盤價 =temp開盤價;
                dayInfo.最高價 = 是否有成交價 ? double.Parse(row[6]):temp開盤價;
                dayInfo.最低價 = 是否有成交價 ? double.Parse(row[7]):temp開盤價;
                dayInfo.收盤價 = 是否有成交價 ? double.Parse(row[8]) : temp開盤價;
                int direction = row[9].Contains("+") ? 1 : -1;
                double 漲跌價差 = double.Parse(row[10]) * direction;
                dayInfo.平盤價 = dayInfo.收盤價 - 漲跌價差;
                dayInfo.漲幅 = 漲跌價差 / dayInfo.平盤價;
                dayInfo.本益比 = double.Parse(row[15]);
                dayInfo.周轉率 = Convert.ToDouble(dayInfo.成交量) / baseInfo.StockAmount;
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
                decimal 當沖成交張數 = decimal.Parse(row[3]) / 1000;
                dayInfo.當沖率 =dayInfo.成交量>0? Convert.ToDouble(當沖成交張數 / dayInfo.成交量) : 0;
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

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後外資資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上市股票盤後外資資訊"] + date.ToDateFormateForTse();
            HttpClient client = _httpClientFactory.CreateClient();
            上市股票盤後外資資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上市股票盤後外資資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.外資買入 = Convert.ToInt32(decimal.Parse(row[2]) / 1000);
                dayInfo.外資賣出 = Convert.ToInt32(decimal.Parse(row[3]) / 1000);
                dayInfo.外資買賣超 = Convert.ToInt32(decimal.Parse(row[4]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後投信資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上市股票盤後投信資訊"] + date.ToDateFormateForTse();
            HttpClient client = _httpClientFactory.CreateClient();
            上市股票盤後投信資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上市股票盤後投信資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.投信買入 = Convert.ToInt32(decimal.Parse(row[3]) / 1000);
                dayInfo.投信賣出 = Convert.ToInt32(decimal.Parse(row[4]) / 1000);
                dayInfo.投信買賣超 = Convert.ToInt32(decimal.Parse(row[5]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後基本資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date, List<StockBaseInfo> baseInfos)
        {
            string url = _config["上櫃股票盤後基本資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後基本資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後基本資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                StockBaseInfo baseInfo = baseInfos.First(b => b.StockId == stockId);
                bool 是否有成交價 = double.TryParse(row[2], out double temp收盤價);
                if(!是否有成交價) temp收盤價= double.Parse(row[16]);   //如果沒有成交量的話，原本收盤價的資料會是---，就取次日參考價
                dayInfo.收盤價 = temp收盤價;
                double 漲跌 =是否有成交價? row[3].ToDouble():0;  //備註: 有時候漲跌資訊會是 "除息"，但不知道為什麼會有成交量，這種情況就...先當作0，ex:112/01/05的6488環球晶
                dayInfo.開盤價 = 是否有成交價 ? double.Parse(row[4]):temp收盤價;
                dayInfo.最高價 = 是否有成交價 ? double.Parse(row[5]):temp收盤價;
                dayInfo.最低價 = 是否有成交價 ? double.Parse(row[6]) : temp收盤價;
                dayInfo.成交量 = 是否有成交價 ? Convert.ToInt32(decimal.Parse(row[8])/1000) :0;
                dayInfo.平盤價 = dayInfo.收盤價 - 漲跌;
                dayInfo.漲幅 = 漲跌 / dayInfo.收盤價;
                dayInfo.周轉率 =Convert.ToDouble(dayInfo.成交量) / baseInfo.StockAmount;
                dayInfo.本益比 = 0;
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後當沖資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後當沖資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後當沖資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後當沖資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                decimal 當沖成交張數 = decimal.Parse(row[3])/1000;
                dayInfo.當沖率 =dayInfo.成交量>0? Convert.ToDouble(當沖成交張數 / dayInfo.成交量):0;
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後融資融券資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後融資融券資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後融資融券資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後融資融券資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.融資買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.融資賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.融資買賣超 = dayInfo.融資買入 - dayInfo.融資賣出;
                dayInfo.融資餘額 = Convert.ToInt32(decimal.Parse(row[6]));
                dayInfo.融券買入 = Convert.ToInt32(decimal.Parse(row[12]));
                dayInfo.融券賣出 = Convert.ToInt32(decimal.Parse(row[11]));
                dayInfo.融券買賣超 = dayInfo.融券買入 - dayInfo.融券賣出;
                dayInfo.融券餘額 = Convert.ToInt32(decimal.Parse(row[14]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後借券資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後借券資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後借券資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後借券資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.借券買入 = Convert.ToInt32(decimal.Parse(row[10]) / 1000);
                dayInfo.借券賣出 = Convert.ToInt32(decimal.Parse(row[9]) / 1000);
                dayInfo.借券買賣超 = dayInfo.借券買入 - dayInfo.借券賣出;
                dayInfo.借券餘額 = Convert.ToInt32(decimal.Parse(row[12]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後外資淨買超資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後外資淨買超資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後外資淨買超資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後外資淨買超資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.外資買入 =Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.外資賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.外資買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後外資淨賣超資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後外資淨賣超資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後外資淨賣超資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後外資淨賣超資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.外資買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.外資賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.外資買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後投信淨買超資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後投信淨買超資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後投信淨買超資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後投信淨買超資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.投信買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.投信賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.投信買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後投信淨賣超資訊(ConcurrentBag<StockDayInfo> dayInfoBags, DateOnly date)
        {
            string url = _config["上櫃股票盤後投信淨賣超資訊"] + date.ToDateFormateForOtc();
            HttpClient client = _httpClientFactory.CreateClient();
            上櫃股票盤後投信淨賣超資訊回傳結果? res = null;
            res = await client.GetFromJsonAsync<上櫃股票盤後投信淨賣超資訊回傳結果>(url);
            ArgumentNullException.ThrowIfNull(res);
            string[][]? datas = res.aaData;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = dayInfoBags.FirstOrDefault(d => d.StockId == stockId);
                if (dayInfo == null) continue;
                dayInfo.投信買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.投信賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.投信買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        //[LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        //protected virtual async Task<T> GetJsonDataByHttpClient<T>(string url)
        //{
        //    HttpClient client = _httpClientFactory.CreateClient();
        //    T? res = default(T);
        //    res = await client.GetFromJsonAsync<T>(url);
        //    ArgumentNullException.ThrowIfNull(res);
        //    return res;
        //}
        #endregion
    }
}
