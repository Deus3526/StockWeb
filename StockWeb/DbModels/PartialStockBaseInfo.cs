using StockWeb.Enums;

namespace StockWeb.DbModels
{
    public partial class StockBaseInfo
    {
        public StockType StockType { get; set; } =StockType.UnKnown;
    }
}
