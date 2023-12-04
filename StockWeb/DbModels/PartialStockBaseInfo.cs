using StockWeb.Enums;

namespace StockWeb.DbModels
{
    public partial class StockBaseInfo
    {
        public StockTypeEnum StockType { get; set; } =StockTypeEnum.UnKnown;

        /// <summary>
        /// 輸入一個新的StockBaseInfo來更新來自DB的StockBaseInfo的資訊，ID要一樣
        /// </summary>
        /// <param name="stockBaseInfo"></param>
        /// <exception cref="Exception"></exception>
        public void UpdateStockBaseInfo(StockBaseInfo stockBaseInfo)
        {
            if (StockId != stockBaseInfo.StockId) throw new Exception("更新StockBaseInfo發生錯誤");
            StockName = stockBaseInfo.StockName;
            Category = stockBaseInfo.Category;
            StockAmount = stockBaseInfo.StockAmount;
            StockType = stockBaseInfo.StockType;
            return;
        }
    }
}
