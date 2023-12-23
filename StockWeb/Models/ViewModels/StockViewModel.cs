namespace StockWeb.Models.ViewModels
{
    public abstract class StockViewModelBase
    {
        public int StockId { get; set; }
        public required string StockName { get; set;}
        public DateOnly Date { get; set; }
    }

    public class Strategy1ViewModel:StockViewModelBase
    {
        public int StockAmount { get; set; }
        public int BuyAmount { get; set; }

        public double BuyRate { get; set; }
    }
}
