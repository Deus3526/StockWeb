using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using StockWeb.DbModels;
using StockWeb.DbModels.DbExtensions;
using StockWeb.Models.ViewModels;
using System.Diagnostics;

namespace StockWeb.Services
{
    public class StockBreakout60MaService : IHostedService
    {
        private readonly StockContext _db;
        private readonly EventBus _eventBus;
        public StockBreakout60MaService(StockContext db, EventBus eventBus)
        {
            _db = db;
            _eventBus = eventBus;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventBus.Subscribe<UpdateDayInfoEvent>(RecordStockBreakout60Table);
            return Task.CompletedTask;
        }

        private async Task RecordStockBreakout60Table(UpdateDayInfoEvent e)
        {
            Console.WriteLine($"{nameof(RecordStockBreakout60Table)} Start...");
            try
            {
                var q1 = await _db.QueryStockDayInfoWithMA_WithLastMa60AndBolling(e.Date);
                var lastDate = (await _db.StockMa60breakoutDays.Where(x => x.Date < e.Date).MaxAsync(x => (DateOnly?)x.Date)) ?? e.Date;
                var q2 = await _db.StockMa60breakoutDays.Where(x => x.Date == lastDate).ToListAsync();
                var list = new List<StockMa60breakoutDay>();
                foreach (var item in q1)
                {
                    if ((item.平盤價 < item.LastMa60 || item.最低價 < item.Ma60) && item.收盤價 >= item.Ma60)
                    {
                        list.Add(new StockMa60breakoutDay
                        {
                            Date = item.Date,
                            DaysSinceBreakout = 0,
                            StockId = item.StockId
                        });
                    }
                    else
                    {
                        var qq = q2.FirstOrDefault(x => x.StockId == item.StockId);
                        if (qq != null)
                        {
                            list.Add(new StockMa60breakoutDay
                            {
                                Date = item.Date,
                                DaysSinceBreakout = qq.DaysSinceBreakout + 1,
                                StockId = item.StockId
                            });
                        }
                    }
                }
                await _db.BulkInsertAsync(list);
                Console.WriteLine($"{nameof(RecordStockBreakout60Table)} Success");
                return;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"{nameof(RecordStockBreakout60Table)} Failed  Date : {e.Date}");
            }
        }

        public async Task<List<StrategyStockBreakoutBollingWithMa60Response>> StrategyStockBreakoutBollingWithMa60(DateOnly date)
        {
            var q1 = (await _db.QueryStockDayInfoWithMA_WithLastMa60AndBolling(date))
                .Where(x => x.Date == date)
                .Where(x => x.平盤價 < x.LastBollingTop && x.收盤價 >= x.BollingTop)
                .ToList();
            var q2 = await _db.StockMa60breakoutDays.Where(x => x.Date == date).ToListAsync();
            var result = new List<StrategyStockBreakoutBollingWithMa60Response>();
            foreach (var item in q1)
            {
                result.Add(new StrategyStockBreakoutBollingWithMa60Response
                {
                    StockId = item.StockId,
                    Price = item.收盤價,
                    DaysSinceBreakout = q2.FirstOrDefault(x => x.StockId == item.StockId)?.DaysSinceBreakout ?? -1,
                    StockName = "aaa",
                    成交量 = item.成交量,
                    漲幅 = item.漲幅,
                    超過布林漲幅 = (item.收盤價 - item.BollingTop) / item.BollingTop
                });
            }
            return result.Where(x => x.DaysSinceBreakout >= 0 && x.DaysSinceBreakout <= 15 && x.成交量 > 500).OrderByDescending(x => x.超過布林漲幅).ToList();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
