using StockWeb.Enums;

namespace StockWeb.DbModels
{
    public partial class StockBaseInfo
    {
        public StockTypeEnum StockType { get; set; } =StockTypeEnum.UnKnown;
    }
}
