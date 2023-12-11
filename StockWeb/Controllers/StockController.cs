using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;
using StockWeb.DbModels;
using StockWeb.Models.ApiResponseModel;
using StockWeb.Models.RequestParms;
using StockWeb.Services.ServicesForControllers;
using StockWeb.StartUpConfigure;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(ApiGroups.Stock))]
    [Tags(Tags.股票相關)]
    public class StockController : ControllerBase
    {
        private readonly StockContext _db;
        private readonly StockService _stockService;
        public StockController(StockContext db,StockService stockService) 
        {
            _db= db;
            _stockService=stockService;
        }
        /// <summary>
        /// 更新DB上市櫃股票基本資訊
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> UpdateStockBaseInfo()
        {
            await _stockService.UpdateStockBaseInfo();
            return Ok();
        }

        /// <summary>
        /// 往前或往後更新股票日成交資訊
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<ActionResult> UpdateStockDayInfo(UpdateStockDayInfoParm parm)
        {
            await _stockService.UpdateStockDayInfo(parm.IsHistoricalUpdate!.Value);
            return Ok();
        }
    }
}
