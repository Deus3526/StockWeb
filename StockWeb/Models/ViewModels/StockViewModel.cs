namespace StockWeb.Models.ViewModels
{
    public abstract class StockViewModelBase
    {
        public int StockId { get; set; }
        public required string StockName { get; set; }
        public DateOnly Date { get; set; }
    }

    public class Strategy1ViewModel : StockViewModelBase
    {
        public int StockAmount { get; set; }
        public int BuyAmount { get; set; }

        public double BuyRate { get; set; }
    }

    public class Strategy8ViewModel
    {
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
    }


    public class StrategyStockBreakoutBollingWithMa60Response
    {
        public int StockId { get; set; }
        public required string StockName { get; set; }
        public int DaysSinceBreakout { get; set; }
        public double Price { get; set; }
        public int 成交量 { get; set; }
        public double 漲幅 { get; set; }
        public double 超過布林漲幅 { get; set; }
    }
}
