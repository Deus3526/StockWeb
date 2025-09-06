using StockWeb.Enums;

namespace StockWeb.DbModels
{
    public partial class StockDayInfo
    {
        public StockDayInfoDataTypeEnum DataType { get; set; } = StockDayInfoDataTypeEnum.UnKnown;
    }
}
