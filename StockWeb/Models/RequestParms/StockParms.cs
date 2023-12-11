using System.ComponentModel.DataAnnotations;

namespace StockWeb.Models.RequestParms
{
    public class UpdateStockDayInfoParm
    {
        /// <summary>
        /// true:更新舊資料、false:更新新資料
        /// </summary>
        /// <example>false</example>
        [Required]
       public bool? IsHistoricalUpdate { get; set; }
    }
}
