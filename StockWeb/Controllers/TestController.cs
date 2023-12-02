using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockWeb.DbModels;
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
        public TestController(StockContext db)
        {
            _db = db;
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
    }
}
