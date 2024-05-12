using System.Globalization;

namespace StockWeb.Extensions
{
    public static class DateTimeExtension
    {
        private static readonly CultureInfo culture;
        static DateTimeExtension()
        {
            culture = new CultureInfo("zh-TW");
            culture.DateTimeFormat.Calendar = new TaiwanCalendar();
        }

        /// <summary>
        /// 返回 20230526的格式
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToDateFormateString1(this DateTime dateTime)
        {
            return dateTime.ToString("yyyyMMdd"); 
        }

        /// <summary>
        /// 返回111/05/26的格式
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static string ToDateFormateString2(this DateTime dateTime)
        {
            return dateTime.ToString("yyy/MM/dd", culture);
        }

        /// <summary>
        /// 返回 20230526的格式
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToDateFormateForTse(this DateOnly date)
        {
            return date.ToString("yyyyMMdd");
        }

        /// <summary>
        /// 返回111/05/26的格式
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string ToDateFormateForOtc(this DateOnly date)
        {
            return date.ToString("yyy/MM/dd", culture);
        }


        /// <summary>
        /// 將 111/05/26的字串轉換為DateOnly物件
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static DateOnly ToDateOnly(this string s)
        {
            DateOnly date = DateOnly.ParseExact(s, "yyy/MM/dd", culture);
            return date;
        }
        public static DateOnly ToDateOnly2(this string s)
        {
            DateOnly date = DateOnly.ParseExact(s, "yyy年MM月dd日", culture);
            return date;
        }
    }
}
