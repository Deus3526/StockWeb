using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using StockWeb.Models.RequestParms;
using StockWeb.Services;
using StockWeb.Services.ServicesForControllers;
using StockWeb.StartUpConfigure;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(ApiGroups.Stock))]
    [Tags(Tags.股票相關)]
    public class StockController(StockService stockService, StockBreakout60MaService stockBreakout60MaService) : ControllerBase
    {
        private readonly StockService _stockService = stockService;
        private readonly StockBreakout60MaService _stockBreakout60MaService = stockBreakout60MaService;

        /// <summary>
        /// 更新DB上市櫃股票基本資訊
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> UpdateStockBaseInfo()
        {
            await _stockService.UpdateStockBaseInfo();
            return NoContent();
        }

        /// <summary>
        /// 往前或往後更新股票日成交資訊
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> UpdateStockDayInfo(UpdateStockDayInfoParm parm)
        {
            //throw new NotImplementedException();
            await _stockService.UpdateStockDayInfo(parm.IsHistoricalUpdate!.Value);
            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult> UpdateDividendYield()
        {
            await _stockService.更新股票殖利率();
            return NoContent();
        }


        /// <summary>
        /// 20個交易日內，投信買超超過總張數1%
        /// </summary>
        /// <param name="date" example="2023-08-01"></param>
        /// <returns></returns>
        //[HttpGet]
        //public async Task<ActionResult> Strategy1([FromQuery]DateTimeOffset date)
        //{
        //    var result=await _stockService.Strategy1(DateOnly.FromDateTime(date.Date));
        //    return Ok(result);
        //}
        [HttpGet]
        [OutputCache(Duration = 600)]// Duration以秒為單位
        public async Task<ActionResult> Strategy1(DateOnly date)
        {
            var result = await _stockService.Strategy1(date);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> StrategyStockBreakoutBollingWithMa60(DateOnly date)
        {
            return Ok(await _stockBreakout60MaService.StrategyStockBreakoutBollingWithMa60(date));
        }
    }
}
