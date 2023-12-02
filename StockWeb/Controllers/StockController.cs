using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockWeb.DbModels;
using StockWeb.Models.ApiResponseModel;
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

        [HttpGet]
        public async  Task<ActionResult<StockBaseInfo>> test()
        {
            var q =  await _db.StockBaseInfos.FirstAsync();
            return Ok(q);
        }

        /// <summary>
        /// 更新DB上市櫃股票基本資訊
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> UpdateStockBaseInfo()
        {
            await _stockService.UpdateStockBaseInfo();
            return Ok();
        }
    }
}
