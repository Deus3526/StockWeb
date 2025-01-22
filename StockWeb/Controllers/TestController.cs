using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StockWeb.DbModels;
using StockWeb.Services.ServicesForControllers;
using StockWeb.StartUpConfigure;
using StockWeb.StartUpConfigure.Middleware;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(ApiGroups.Test))]
    [Tags("測試")]
    public class TestController : ControllerBase
    {
        private readonly StockContext _db;
        private readonly StockSource _source;
        private readonly StockService _stockService;
        public TestController(StockContext db, IOptions<StockSource> source, StockService stockService)
        {
            _db = db;
            _source = source.Value;
            _stockService = stockService;
        }

        [HttpPost]
        public ActionResult<StockBaseInfo> testNeedLogin()
        {
            var q = _db.StockBaseInfos.First();
            return q;
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult<StockBaseInfo> testNoNeedLogin()
        {
            var q = _db.StockBaseInfos.First();
            return q;
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult testCustomExceptionResponse()
        {
            throw new CustomErrorResponseException("自訂錯誤回傳測試", StatusCodes.Status204NoContent);
            //return Ok();
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult ExceptionLogTest()
        {
            int? a = null;
            ArgumentNullException.ThrowIfNull(a);
            return Ok();
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> test月營收(DateOnly date)
        {
            await _stockService.更新月營收資訊(date);
            return Ok();
        }
    }
}
