namespace StockWeb.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// 嘗試轉換成double，如果轉換失敗回傳0
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double ToDouble(this string value)
        {
            if (!double.TryParse(value, out double result))
            {
                result = 0;
            }
            return result;
        }
    }
}
