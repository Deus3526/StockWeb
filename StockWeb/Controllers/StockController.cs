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
        /// <summary>
        /// 營收三紅突破布林，融資增or外資買
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy8(DateOnly date)
        {
            var result = await _stockService.Strategy8(date);
            return Ok(result);
        }
        /// <summary>
        /// 營收三紅，穿過季線，且融資增or外資買
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy9(DateOnly date)
        {
            var result = await _stockService.Strategy9(date);
            return Ok(result);
        }
        /// <summary>
        /// 營收三紅，穿過某一條均線或布林
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy10(DateOnly date)
        {
            var result = await _stockService.Strategy10(date);
            return Ok(result);
        }
        /// <summary>
        /// 營收三紅，融資創10天內最高的兩倍
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy11(DateOnly date)
        {
            var result = await _stockService.Strategy11(date);
            return Ok(result);
        }
        /// <summary>
        /// 過去10天表現都贏過大盤
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy12(DateOnly date)
        {
            var result = await _stockService.Strategy12(date);
            return Ok(result);
        }
        /// <summary>
        /// 布林上、季線多頭且當天漲幅3%以上或前一天漲停
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy14(DateOnly date)
        {
            var result = await _stockService.Strategy14(date);
            return Ok(result);
        }
        /// <summary>
        ///  與strategy14類似，但是布林跟ma20差距在10%以內(通道緊縮)
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Strategy15(DateOnly date)
        {
            var result = await _stockService.Strategy15(date);
            return Ok(result);
        }
    }
}
