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

    public class Strategy9ViewModel
    {
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
    }
    public class Strategy10ViewModel
    {
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
    }
    public class Strategy11ViewModel
    {
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
        public double Mom { get; set; }
        public double Yoy { get; set; }
        public double 累計Yoy { get; set; }
    }
    public class Strategy12ViewModel
    {
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
        public double Mom { get; set; }
        public double Yoy { get; set; }
        public double 累計Yoy { get; set; }
        public double sum漲幅 { get; set; }
        public double 漲幅 { get; set; }
        public double Lag漲幅1 { get; set; }
        public double Lag漲幅2 { get; set; }
        public double Lag漲幅3 { get; set; }
        public double Lag漲幅4 { get; set; }
        public double Lag漲幅5 { get; set; }
        //public double Lag漲幅6 { get; set; }
        //public double Lag漲幅7 { get; set; }
        //public double Lag漲幅8 { get; set; }
        //public double Lag漲幅9 { get; set; }
        //public double Lag漲幅10 { get; set; }
        public double 大盤漲幅 { get; set; }
        public double Lag大盤漲幅1 { get; set; }
        public double Lag大盤漲幅2 { get; set; }
        public double Lag大盤漲幅3 { get; set; }
        public double Lag大盤漲幅4 { get; set; }
        public double Lag大盤漲幅5 { get; set; }
        //public double Lag大盤漲幅6 { get; set; }
        //public double Lag大盤漲幅7 { get; set; }
        //public double Lag大盤漲幅8 { get; set; }
        //public double Lag大盤漲幅9 { get; set; }
        //public double Lag大盤漲幅10 { get; set; }
    }
    public class Strategy14ViewModel
    {
        public required string StockName { get; set; }
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
        public double 收盤價 { get; set; }
        public double 掛買 => 收盤價 * 1.03;
        public double 掛賣 => 收盤價 * 1.06;
    }
    public class Strategy15ViewModel
    {
        public required string StockName { get; set; }
        public int StockId { get; set; }
        public DateOnly Date { get; set; }
        public double 收盤價 { get; set; }
        public double 掛買 => 收盤價 * 1.03;
        public double 掛賣 => 收盤價 * 1.06;
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
