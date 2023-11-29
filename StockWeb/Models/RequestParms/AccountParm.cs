using System.ComponentModel.DataAnnotations;

namespace StockWeb.Models.RequestParms
{
    public class LoginParm
    {
        /// <summary>
        /// 
        /// </summary>
        /// <example>deus.ko3526</example>
        [Required]
        public string account { get; set; }=string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <example>deus.ko3526</example>
        [Required]
        public string password { get; set; }=string.Empty;
    }

    public class RegisterAccountParm
    {
        [Required]
        public string account { get; set; } = string.Empty;

        [Required]
        public string password { get; set; } = string.Empty;

        [Required]
        public string userName { get; set; } = string.Empty;
    }
}
