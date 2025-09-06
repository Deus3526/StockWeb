using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using StockWeb.DbModels;
using StockWeb.Enums;
using StockWeb.Extensions;
using StockWeb.Models.ApiResponseModel;
using StockWeb.Models.ViewModels;
using StockWeb.StartUpConfigure;
using StockWeb.StartUpConfigure.Middleware;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StockWeb.Services.ServicesForControllers
{
    public class StockService(StockContext db, ILogger<StockService> logger, RequestApiService requestApiService, IOptions<StockSource> stockSource, IMemoryCache cache, EventBus eventBus)
    {
        private readonly StockContext _db = db;
        private readonly RequestApiService _requestApiService = requestApiService;
        private readonly ILogger<StockService> _logger = logger;
        private readonly StockSource _stockSource = stockSource.Value;
        private readonly EventBus _eventBus = eventBus;
        /// <summary>
        /// 因為刪除資料跟更新上市與上櫃資料的時候可能都會用到DbContext，這樣同一個實例的DbContext會打架，要馬用非同步鎖鎖住，要馬注入ServiceScopeFactory來CeateScope取得新的DbContext
        /// </summary>
        private readonly SemaphoreSlim _semaphoreSlimForDbContext = new(1, 1);
        private readonly IMemoryCache _cache = cache;
        private Dictionary<int, StockBaseInfo>? _baseInfos = null;

        private Dictionary<int, StockBaseInfo> GetStockBaseInfos()
        {
            if (_baseInfos == null)
            {
                var baseInfos = _db.StockBaseInfos.AsNoTracking().ToDictionary(s => s.StockId, s => s);
                _baseInfos = baseInfos;
            }
            return _baseInfos;
        }

        #region 更新股票基本資訊表

        /// <summary>
        /// 更新股票基本資訊表
        /// </summary>
        /// <returns></returns>
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        public virtual async Task UpdateStockBaseInfo()
        {
            Dictionary<int, StockBaseInfo> BaseInfos = await _db.StockBaseInfos.ToDictionaryAsync(s => s.StockId, s => s);
            ConcurrentDictionary<int, StockBaseInfo> concurrentBaseInfos = new();
            List<Task> tasks =
            [
                更新上市股票基本訊息_計算流通張數(concurrentBaseInfos),
                更新上櫃股票基本訊息_發行股數(concurrentBaseInfos)
            ];

            await Task.WhenAll(tasks);
            foreach (var kvp in concurrentBaseInfos)
            {
                StockBaseInfo? baseInfo = BaseInfos.GetValueOrDefault(kvp.Key);
                if (baseInfo == null)
                {
                    _db.StockBaseInfos.Add(kvp.Value);
                }
                else
                {
                    baseInfo.UpdateStockBaseInfo(kvp.Value);
                }
            }
            await _db.SaveChangesAsync();
            return;
        }

        /// <summary>
        /// 更新當天的即時股價
        /// </summary>
        /// <returns></returns>
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        public virtual async Task UpdateStockDayInfoTempToday()
        {
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票基本訊息_計算流通張數(ConcurrentDictionary<int, StockBaseInfo> concurrentBaseInfos)
        {
            var res = await _requestApiService.GetFromJsonAsync<List<上市股票基本資訊>>(nameof(_stockSource.OpenapiTwse), _stockSource.OpenapiTwse.Route.上市股票基本訊息_計算流通張數);
            foreach (上市股票基本資訊 s in res)
            {
                ArgumentNullException.ThrowIfNull(s.公司代號);
                int stockId = s.公司代號.Value;
                StockBaseInfo? baseInfo = concurrentBaseInfos.GetValueOrDefault(stockId);
                if (baseInfo == null)
                {
                    baseInfo = new StockBaseInfo
                    {
                        StockId = stockId,
                        StockType = StockTypeEnum.tse
                    };
                    concurrentBaseInfos[stockId] = baseInfo;
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
        protected virtual async Task 更新上櫃股票基本訊息_發行股數(ConcurrentDictionary<int, StockBaseInfo> concurrentBaseInfos)
        {
            var res = await _requestApiService.GetFromJsonAsync<上櫃股票基本資訊_發行股數回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票基本訊息_發行股數);
            res.轉換為上櫃股票基本資訊_流通股數(concurrentBaseInfos);
            return;
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
            DateOnly date = await GetDateMaxOrMinFromStockDayInfoAsync(isHistoricalUpdate);
            date = await 取得與參數日期最近的開市日期_若無資料則更新上市大盤盤後資訊(date);
            _logger.LogInformation("開始更新日成交資訊 : {date}", date);

            await UpdateStockDayInfoByDate(date);

            _ = _eventBus.PublishAsync(new UpdateDayInfoEvent { Date = date });
        }
        /// <summary>
        /// 取得StockDayInfo最小或最大的交易日期
        /// </summary>
        /// <param name="isHistoricalUpdate"></param>
        /// <returns></returns>
        private async Task<DateOnly> GetDateMaxOrMinFromStockDayInfoAsync(bool isHistoricalUpdate)
        {
            DateOnly? date = null;  //如果Db完全沒有日成交資料，從2021/01/04開始計算
            if (isHistoricalUpdate)
            {
                date = await _db.StockDayInfos.MinAsync(s => (DateOnly?)s.Date);
            }
            else
            {
                date = await _db.StockDayInfos.MaxAsync(s => (DateOnly?)s.Date);
            }
            if (date == null) date = new DateOnly(2021, 1, 4);  //如果Db完全沒有日成交資料，從2021/01/04開始計算
            return date.Value;
        }


        /// <summary>
        /// 更新指定日期的日成交資訊
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public virtual async Task UpdateStockDayInfoByDate(DateOnly date)
        {
            var concurrentDayInfos = await _db.StockBaseInfos.AsNoTracking().Select(s => new StockDayInfo { StockId = s.StockId, Date = date,DataType=StockDayInfoDataTypeEnum.盤後 }).ToConcurrentDictionaryAsync(s => s.StockId, s => s);
            var baseInfos = await _db.StockBaseInfos.AsNoTracking().ToDictionaryAsync(s => s.StockId, s => s);
            List<Task> tasks =
            [
                DeleteDayInfoRange(date, date),
                UpdateTseDayInfo(concurrentDayInfos, date, baseInfos),
                UpdateOtcDayInfo(concurrentDayInfos, date, baseInfos),
            ];
            await Task.WhenAll(tasks);

            foreach (var kvp in concurrentDayInfos)
            {
                //如果是成交量為0的話，還是會有資料，這樣在前面update的時候會取得上一個交易日的收盤價
                //但是如果是減資之類的停止交易的狀況 或是 未上市或已下市，不會有資料，此時收盤價會是0，不視為交易日
                if (kvp.Value.收盤價 == 0) concurrentDayInfos.Remove(kvp.Key, out StockDayInfo? dayInfo);
            }
            // 啟動計時器
            Stopwatch stopwatch = Stopwatch.StartNew();
            using var transaction = await _db.Database.BeginTransactionAsync();
            //_db.StockDayInfos.AddRange(concurrentDayInfos.Values);
            //await _db.SaveChangesAsync(); //3293毫秒
            await _db.BulkInsertAsync(concurrentDayInfos.Values); //237毫秒
            // 停止計時器
            stopwatch.Stop();
            Console.WriteLine($"插入資料耗費 : {stopwatch.ElapsedMilliseconds} 毫秒");
            //stopwatch.Restart();
            //await UpdateMovingAverage(date);
            //stopwatch.Stop();
            //Console.WriteLine($"更新日線資料耗費 : {stopwatch.ElapsedMilliseconds} 毫秒");
            await transaction.CommitAsync();

            await 更新月營收資訊(date);
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
            await _semaphoreSlimForDbContext.WaitAsync(); // 等待獲得信號量
            try
            {
                await _db.StockDayInfos.Where(s =>
                     (startDate == null || s.Date >= startDate) &&
                     (endDate == null || s.Date <= endDate)
                 ).ExecuteDeleteAsync();
                return;
            }
            finally
            {
                _semaphoreSlimForDbContext.Release(); // 完成後釋放信號量
            }

        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task UpdateTseDayInfo(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfo, DateOnly date, Dictionary<int, StockBaseInfo> baseInfos)
        {
            List<Task> tasks =
            [
                //當沖率計算需要基本資訊的成交量，所以要確保在其之後執行
                Task.Run(async () =>
                {
                    await 更新上市股票盤後基本資訊(concurrentDayInfo, date, baseInfos);
                    await 更新上市股票盤後當沖資訊(concurrentDayInfo, date);
                }),
                更新上市股票盤後融資融券資訊(concurrentDayInfo, date),
                更新上市股票盤後借券資訊(concurrentDayInfo, date),
                更新上市股票盤後外資資訊(concurrentDayInfo, date),
                更新上市股票盤後投信資訊(concurrentDayInfo, date),
            ];

            await Task.WhenAll(tasks);

        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task UpdateOtcDayInfo(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date, Dictionary<int, StockBaseInfo> baseInfos)
        {
            List<Task> tasks =
            [
                //當沖率計算需要基本資訊的成交量，所以要確保在其之後執行
                Task.Run(async () =>
                {
                    await 更新上櫃股票盤後基本資訊(concurrentDayInfos, date, baseInfos);
                    await 更新上櫃股票盤後當沖資訊(concurrentDayInfos, date);
                }),
                更新上櫃股票盤後融資融券資訊(concurrentDayInfos, date),
                更新上櫃股票盤後借券資訊(concurrentDayInfos, date),
                更新上櫃股票盤後外資淨買超資訊(concurrentDayInfos, date),
                更新上櫃股票盤後外資淨賣超資訊(concurrentDayInfos, date),
                更新上櫃股票盤後投信淨買超資訊(concurrentDayInfos, date),
                更新上櫃股票盤後投信淨賣超資訊(concurrentDayInfos, date),
            ];
            await Task.WhenAll(tasks);
        }


        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後基本資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date, Dictionary<int, StockBaseInfo> baseInfos)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.PostFromJsonAsync<上市股票盤後基本資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票盤後基本資訊, parms, HttpContentType.FormData, "更新上市股票盤後基本資訊");
            string[][]? datas = res.tables[8].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                StockBaseInfo baseInfo = baseInfos[stockId];
                dayInfo.成交量 = Convert.ToInt32(decimal.Parse(row[2])) / 1000;
                bool 是否有成交價 = double.TryParse(row[5], out double temp開盤價);
                if (!是否有成交價) temp開盤價 = await 取得上一個交易日的收盤價(stockId, date); //如果沒有成交價格，上用上一個交易日的收盤價
                dayInfo.開盤價 = temp開盤價;
                dayInfo.最高價 = 是否有成交價 ? double.Parse(row[6]) : temp開盤價;
                dayInfo.最低價 = 是否有成交價 ? double.Parse(row[7]) : temp開盤價;
                dayInfo.收盤價 = 是否有成交價 ? double.Parse(row[8]) : temp開盤價;
                int direction = row[9].Contains('+') ? 1 : -1;
                double 漲跌價差 = double.Parse(row[10]) * direction;
                dayInfo.平盤價 = dayInfo.收盤價 - 漲跌價差;
                dayInfo.漲幅 = dayInfo.平盤價 == 0 ? 0 : 漲跌價差 / dayInfo.平盤價;
                dayInfo.本益比 = double.Parse(row[15]);
                dayInfo.周轉率 = Convert.ToDouble(dayInfo.成交量) / baseInfo.StockAmount;
            }
            return;

        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後當沖資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.GetFromJsonAsync<上市股票盤後當沖資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票盤後當沖資訊, parms);
            string[][]? datas = res.tables[1].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                decimal 當沖成交張數 = decimal.Parse(row[3]) / 1000;
                dayInfo.當沖率 = dayInfo.成交量 > 0 ? Convert.ToDouble(當沖成交張數 / dayInfo.成交量) : 0;
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後融資融券資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.GetFromJsonAsync<上市股票盤後融資融券資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票盤後融資融券資訊, parms);

            string[][]? datas = res.tables[1].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.融資買入 = Convert.ToInt32(decimal.Parse(row[2]));
                dayInfo.融資賣出 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.融資買賣超 = dayInfo.融資買入 - dayInfo.融資賣出;
                dayInfo.融資餘額 = Convert.ToInt32(decimal.Parse(row[6]));
                dayInfo.融券買入 = Convert.ToInt32(decimal.Parse(row[8]));
                dayInfo.融券賣出 = Convert.ToInt32(decimal.Parse(row[9]));
                dayInfo.融券買賣超 = dayInfo.融券買入 - dayInfo.融券賣出;
                dayInfo.融券餘額 = Convert.ToInt32(decimal.Parse(row[12]));
            }
            return;
        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後借券資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.GetFromJsonAsync<上市股票盤後借券資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票盤後借券資訊, parms);
            string[][]? datas = res.data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.借券買入 = Convert.ToInt32(decimal.Parse(row[10]) / 1000);
                dayInfo.借券賣出 = Convert.ToInt32(decimal.Parse(row[9]) / 1000);
                dayInfo.借券買賣超 = dayInfo.借券買入 - dayInfo.借券賣出;
                dayInfo.借券餘額 = Convert.ToInt32(decimal.Parse(row[12]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後外資資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.GetFromJsonAsync<上市股票盤後外資資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票盤後外資資訊, parms);
            string[][]? datas = res.data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.外資買入 = Convert.ToInt32(decimal.Parse(row[9]) / 1000);
                dayInfo.外資賣出 = Convert.ToInt32(decimal.Parse(row[10]) / 1000);
                dayInfo.外資買賣超 = Convert.ToInt32(decimal.Parse(row[11]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上市股票盤後投信資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.GetFromJsonAsync<上市股票盤後投信資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票盤後投信資訊, parms);
            string[][]? datas = res.data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.投信買入 = Convert.ToInt32(decimal.Parse(row[3]) / 1000);
                dayInfo.投信賣出 = Convert.ToInt32(decimal.Parse(row[4]) / 1000);
                dayInfo.投信買賣超 = Convert.ToInt32(decimal.Parse(row[5]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後基本資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date, Dictionary<int, StockBaseInfo> baseInfos)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後基本資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後基本資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                StockBaseInfo baseInfo = baseInfos[stockId];
                bool 是否有成交價 = double.TryParse(row[2], out double temp收盤價);
                if (!是否有成交價) temp收盤價 = await 取得上一個交易日的收盤價(stockId, date); //如果沒有成交價格，上用上一個交易日的收盤價
                dayInfo.收盤價 = temp收盤價;
                double 漲跌 = 是否有成交價 ? row[3].ToDouble() : 0;  //備註: 有時候漲跌資訊會是 "除息"，但不知道為什麼會有成交量，這種情況就...先當作0，ex:112/01/05的6488環球晶
                dayInfo.開盤價 = 是否有成交價 ? double.Parse(row[4]) : temp收盤價;
                dayInfo.最高價 = 是否有成交價 ? double.Parse(row[5]) : temp收盤價;
                dayInfo.最低價 = 是否有成交價 ? double.Parse(row[6]) : temp收盤價;
                dayInfo.成交量 = 是否有成交價 ? Convert.ToInt32(decimal.Parse(row[8]) / 1000) : 0;
                dayInfo.平盤價 = dayInfo.收盤價 - 漲跌;
                dayInfo.漲幅 = dayInfo.平盤價 == 0 ? 0 : 漲跌 / dayInfo.平盤價;
                dayInfo.周轉率 = Convert.ToDouble(dayInfo.成交量) / baseInfo.StockAmount;
                dayInfo.本益比 = 0;
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後當沖資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() },
                {"type","Daily" }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後當沖資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後當沖資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                decimal 當沖成交張數 = decimal.Parse(row[3]) / 1000;
                dayInfo.當沖率 = dayInfo.成交量 > 0 ? Convert.ToDouble(當沖成交張數 / dayInfo.成交量) : 0;
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後融資融券資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後融資融券資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後融資融券資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
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
        protected virtual async Task 更新上櫃股票盤後借券資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後借券資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後借券資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[0], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.借券買入 = Convert.ToInt32(decimal.Parse(row[10]) / 1000);
                dayInfo.借券賣出 = Convert.ToInt32(decimal.Parse(row[9]) / 1000);
                dayInfo.借券買賣超 = dayInfo.借券買入 - dayInfo.借券賣出;
                dayInfo.借券餘額 = Convert.ToInt32(decimal.Parse(row[12]) / 1000);
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後外資淨買超資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() },
                {"searchType","buy" },
                {"type","Daily" }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後外資淨買超資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後外資淨買超資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.外資買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.外資賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.外資買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後外資淨賣超資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() },
                {"searchType","sell" },
                {"type","Daily" }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後外資淨賣超資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後外資淨賣超資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.外資買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.外資賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.外資買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後投信淨買超資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() },
                {"searchType","buy" },
                {"type","Daily" }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後投信淨買超資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後投信淨買超資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.投信買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.投信賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.投信買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task 更新上櫃股票盤後投信淨賣超資訊(ConcurrentDictionary<int, StockDayInfo> concurrentDayInfos, DateOnly date)
        {
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForOtc() },
                {"searchType","sell" },
                {"type","Daily" }
            };
            var res = await _requestApiService.PostFromJsonAsync<上櫃股票盤後投信淨賣超資訊回傳結果>(nameof(_stockSource.Tpex), _stockSource.Tpex.Route.上櫃股票盤後投信淨賣超資訊, parms, HttpContentType.FormData);
            string[][]? datas = res.tables[0].data;
            ArgumentNullException.ThrowIfNull(datas);
            foreach (var row in datas)
            {
                if (int.TryParse(row[1], out var stockId) == false) continue;
                StockDayInfo? dayInfo = concurrentDayInfos.GetValueOrDefault(stockId);
                if (dayInfo == null) continue;
                dayInfo.投信買入 = Convert.ToInt32(decimal.Parse(row[3]));
                dayInfo.投信賣出 = Convert.ToInt32(decimal.Parse(row[4]));
                dayInfo.投信買賣超 = Convert.ToInt32(decimal.Parse(row[5]));
            }
            return;
        }

        [LoggingInterceptor]
        protected virtual async Task UpdateMovingAverage(DateOnly date)
        {
            ////使用 Zomp.EFCore.WindowFunctions.SqlServer 來在C#使用窗口函數更新日線....布林通道的標準差不支持，而且需要先ToList在下第二個where，不然兩個where條件會合併，所以沒寫成功，還是先暫時用預存程序的方式
            //List<StockDayInfo> dayInfos = _db.StockDayInfos.Where(s => s.Date == date).ToList();
            //var movingAverages = _db.StockDayInfos.Where(s => s.Date > date.AddYears(-1).AddMonths(-1))
            //    .Select(s => new
            //    {
            //        s.StockId,
            //        s.Date,
            //        Ma5 = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(4).ToCurrentRow()),
            //        Ma10 = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(9).ToCurrentRow()),
            //        Ma20 = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(19).ToCurrentRow()),
            //        Ma60 = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(59).ToCurrentRow()),
            //        Ma120 = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(119).ToCurrentRow()),
            //        Ma240 = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(239).ToCurrentRow()),
            //        //BollingTop = EF.Functions.Avg(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(19).ToCurrentRow()) + 2 * EF.Functions.StandardDeviationPopulation(s.收盤價, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(19).ToCurrentRow())
            //    }).ToList().Where(s => s.Date == date);

            ////var s = movingAverages.ToQueryString();
            //foreach (var ma in movingAverages)
            //{
            //    StockDayInfo dayInfo = dayInfos.Find(d => d.StockId == ma.StockId)!;
            //    dayInfo.Ma5 = ma.Ma5;
            //    dayInfo.Ma10 = ma.Ma10;
            //    dayInfo.Ma20 = ma.Ma20;
            //    dayInfo.Ma60 = ma.Ma60;
            //    dayInfo.Ma120 = ma.Ma120;
            //    dayInfo.Ma240 = ma.Ma240;
            //    //dayInfo.BollingTop = ma.BollingTop;
            //}
            //await _db.SaveChangesAsync();

            //考慮使用raw sql用預存程序，這邊用ef的窗口函數寫法好像一定要把資料全部載回來才行，沒辦法分子查詢
            await _db.Database.ExecuteSqlInterpolatedAsync($"EXEC UpdateStockDayInfo_MA_field {date}");

            //預存程序:
            //WITH CTE AS(
            //    SELECT

            //        StockID, date,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 4 PRECEDING AND CURRENT ROW) as ma5,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 9 PRECEDING AND CURRENT ROW) as ma10,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 19 PRECEDING AND CURRENT ROW) as ma20,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 59 PRECEDING AND CURRENT ROW) as ma60,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 119 PRECEDING AND CURRENT ROW) as ma120,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 239 PRECEDING AND CURRENT ROW) as ma240,
            //        avg(收盤價) over(partition by stockid order by date asc rows between 19 PRECEDING AND CURRENT ROW) + 2 * stdevp(收盤價) over(partition by stockid order by date asc rows between 19 PRECEDING and CURRENT ROW) as bollingTop

            //    FROM StockDayInfo

            //    WHERE Date <= @date
            //)
            //UPDATE StockDayInfo
            //SET
            //    Ma5 = CTE.ma5,
            //    Ma10 = CTE.ma10,
            //    Ma20 = CTE.ma20,
            //    Ma60 = CTE.ma60,
            //    Ma120 = CTE.ma120,
            //    Ma240 = CTE.ma240,
            //    BollingTop = CTE.bollingTop
            //    -- 其他字段更新
            //FROM StockDayInfo sdi
            //INNER JOIN CTE ON sdi.StockID = CTE.StockID AND sdi.Date = CTE.Date
            //WHERE sdi.Date = @date;
        }

        [LoggingInterceptor]
        public virtual async Task 更新月營收資訊(DateOnly date)
        {
            var 營收date = date.AddMonths(-1);
            var q = await _requestApiService.Get月營收(營收date, StockTypeEnum.tse);
            var q2 = await _requestApiService.Get月營收(營收date, StockTypeEnum.otc);
            q.AddRange(q2);


            var db營收Data = await _db.月營收s.Where(x => x.Year == 營收date.Year && x.Month == 營收date.Month).ToDictionaryAsync(x => x.StockId, x => x);

            List<月營收> list = new List<月營收>();
            foreach (var item in q)
            {
                if (!int.TryParse(item.StockId, out var stockId)) continue;
                if (db營收Data.ContainsKey(stockId)) continue;
                var entity = new 月營收
                {
                    MoM = item.MOM月增率,
                    Month = 營收date.Month,
                    Year = 營收date.Year,
                    StockId = stockId,
                    YoY = item.YoY年增率,
                    累計yoY = item.累計Yoy
                };
                list.Add(entity);
            }

            await _db.BulkInsertAsync(list);
            return;
        }


        [LoggingInterceptor]
        protected virtual async Task<DateOnly> 取得與參數日期最近的開市日期_若無資料則更新上市大盤盤後資訊(DateOnly date)
        {
            //先找資料庫是否有更大日期的的大盤資料，沒有的話則更新這個月大盤資料
            DateOnly? result = await _db.MarketDayInfos.Where(m => m.Date > date).Select(m => (DateOnly?)m.Date).MinAsync();
            if (result != null) return result.Value;
            List<DateOnly> marketDayInfosDatesUpdated = (await 更新上市大盤盤後資訊_以月為單位(date)).Select(m => m.Date).ToList();


            //更新月份後，此月份是否有下一個交易日，如果沒有的話要再更新下個月的大盤資料
            result = marketDayInfosDatesUpdated.Where(m => m > date).Select(m => (DateOnly?)m).Min();
            if (result != null) return result.Value;

            //用下一個月的1號來更新下個月的大盤資料
            var nextMonthDate = (new DateOnly(date.Year, date.Month, 1)).AddMonths(1);
            marketDayInfosDatesUpdated = (await 更新上市大盤盤後資訊_以月為單位(nextMonthDate)).Select(m => m.Date).ToList();
            result = marketDayInfosDatesUpdated.Where(m => m > date).Select(m => (DateOnly?)m).Min();
            if (result != null) return result.Value;

            throw new CustomErrorResponseException("已經是最新的交易日了");
        }

        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        protected virtual async Task<List<MarketDayInfo>> 更新上市大盤盤後資訊_以月為單位(DateOnly date)
        {
            await _db.MarketDayInfos.Where(m => m.Date.Year == date.Year && m.Date.Month == date.Month).ExecuteDeleteAsync();
            var parms = new Dictionary<string, string?>
            {
                { "date", date.ToDateFormateForTse() }
            };
            var res = await _requestApiService.GetFromJsonAsync<上市大盤成交資訊回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市大盤成交資訊, parms);

            List<MarketDayInfo> marketDayInfos = res.ToMarketDayInfo();
            _db.MarketDayInfos.AddRange(marketDayInfos);
            await _db.SaveChangesAsync();
            return marketDayInfos;
        }

        /// <summary>
        /// 有時候成交量是0的時候，Api回傳的收盤價會是--，這個時候就使用這個method從資料庫拿前一個交易的收盤價
        /// </summary>
        /// <param name="stockId"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        private async Task<double> 取得上一個交易日的收盤價(int stockId, DateOnly date)
        {
            await _semaphoreSlimForDbContext.WaitAsync(); // 等待獲得信號量
            try
            {
                var q = await _db.StockDayInfos.AsNoTracking()
                    .Where(s => s.StockId == stockId && s.Date < date)
                    .OrderByDescending(s => s.Date)
                    .Select(s => (double?)s.收盤價)
                    .FirstOrDefaultAsync();
                if (q == null) return -1;
                else return q.Value;
            }
            finally
            {
                _semaphoreSlimForDbContext.Release(); // 完成後釋放信號量
            }
        }


        #endregion

        #region 更新殖利率
        public async Task 更新股票殖利率()
        {
            var dataTse = await 更新上市股票殖利率(2022);
            return;
        }
        [LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        public async Task<List<DividendYield>> 更新上市股票殖利率(int year)
        {
            List<DividendYield> result = new List<DividendYield>();
            var starDate = new DateOnly(year, 1, 1);
            var endDate = (new DateOnly(year + 1, 1, 1)).AddDays(-1);
            var parms = new Dictionary<string, string?>
            {
                {"startDate",starDate.ToDateFormateForTse() },
                {"endDate", endDate.ToDateFormateForTse()}
            };
            var res = await _requestApiService.GetFromJsonAsync<上市股票殖利率回傳結果>(nameof(_stockSource.Twse), _stockSource.Twse.Route.上市股票殖利率資訊, parms, nameof(更新上市股票殖利率));
            foreach (var row in res.data)
            {
                var baseInfos = GetStockBaseInfos();
                if (int.TryParse(row[1], out var stockId) == false) continue;
                if (baseInfos[stockId] == null || row[6] != "息") continue;
                double price = row[3].ToDouble();
                DividendYield yield = new DividendYield
                {
                    StockId = stockId,
                    PayDate = row[0].ToDateOnly2(),
                    Payment = row[5].ToDouble(),
                };
                yield.DividendYieldRate = yield.Payment / price;
                result.Add(yield);
            }
            return result;
        }

        //[LoggingInterceptor(StatusCode = StatusCodes.Status502BadGateway)]
        //public async Task<List<DividendYield>> 更新上櫃股票殖利率(int year)
        //{

        //}
        #endregion

        #region 股票條件篩選過濾
        public async Task<List<Strategy1ViewModel>> Strategy1(DateOnly date)
        {
            //var q = _db.StockDayInfos.Include(s => s.Stock).AsNoTracking().Where(s => s.Date <= date).OrderBy(s=>s.StockId).ThenBy(s=>s.Date).Select(s => new Strategy1ViewModel
            //{
            //    StockId = s.StockId,
            //    StockName = s.Stock.StockName,
            //    StockAmount=s.Stock.StockAmount,
            //    BuyAmount=EF.Functions.Sum(s.投信買賣超, EF.Functions.Over().PartitionBy(s.StockId).OrderBy(s.Date).Rows().FromPreceding(19).ToCurrentRow())!.Value,
            //    Date=s.Date
            //}).AsSingleQuery();
            //var q2 = q.Where(r => r.BuyAmount >= r.StockAmount * 0.01 && r.Date==date).ToList();
            //return q2.ToList();
            DateOnly dateUpperLimit = date.AddDays(-40);
            var q = await _db.Database.SqlQuery<Strategy1ViewModel>($"exec Strategy1 @date={date},@dateUpperLimit={dateUpperLimit} ").ToListAsync();
            //return q.OrderByDescending(x=>x.BuyRate).ToList();
            return q;

        }

        public async Task<List<Strategy8ViewModel>> Strategy8(DateOnly date)
        {

            var q = await _db.Database.SqlQuery<Strategy8ViewModel>($"exec Strategy8 @date={date} ").ToListAsync();
            return q;

        }
        public async Task<List<Strategy9ViewModel>> Strategy9(DateOnly date)
        {

            var q = await _db.Database.SqlQuery<Strategy9ViewModel>($"exec Strategy9 @date={date} ").ToListAsync();
            return q;

        }

        public async Task<List<Strategy10ViewModel>> Strategy10(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy10ViewModel>($"exec Strategy10 @date={date} ").ToListAsync();
            return q;
        }
        public async Task<List<Strategy11ViewModel>> Strategy11(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy11ViewModel>($"exec Strategy11 @date={date} ").ToListAsync();
            return q;
        }

        public async Task<List<Strategy12ViewModel>> Strategy12(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy12ViewModel>($"exec Strategy12 @date={date} ").ToListAsync();
            return q;
        }
        public async Task<List<Strategy14ViewModel>> Strategy14(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy14ViewModel>($"exec Strategy14 @date={date} ").ToListAsync();
            return q;
        }
        public async Task<List<Strategy15ViewModel>> Strategy15(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy15ViewModel>($"exec Strategy15 @date={date} ").ToListAsync();
            return q;
        }

        public async Task<List<Strategy16ViewModel>> Strategy16(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy16ViewModel>($"exec Strategy16 @date={date} ").ToListAsync();
            return q;
        }
        public async Task<List<Strategy17ViewModel>> Strategy17(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy17ViewModel>($"exec Strategy17 @date={date} ").ToListAsync();
            return q;
        }
        public async Task<List<Strategy18ViewModel>> Strategy18(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy18ViewModel>($"exec Strategy18 @date={date} ").ToListAsync();
            return q;
        }
        public async Task<List<Strategy19ViewModel>> Strategy19(DateOnly date)
        {
            var q = await _db.Database.SqlQuery<Strategy19ViewModel>($"exec Strategy19 @date={date} ").ToListAsync();
            return q;
        }
        #endregion

    }
}
