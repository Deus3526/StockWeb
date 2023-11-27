using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using StockWeb.DbModels;
using StockWeb.Enums;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly ILogger<TestController> _logger;
        public TestController(ILogger<TestController> logger) 
        {
            _logger = logger;
        }

        [HttpGet]
        public ActionResult<StockBaseInfo> test()
        {
            // 示範物件
            var myObject = new
            {
                Name = "Example",
                Value = 42
            };

            // 記錄物件為 JSON 格式
            _logger.LogInformation($"Logging an object: {{@MyObject}},{{@{nameof(LogTypeEnum)}}}", myObject,LogTypeEnum.Request);
            StockContext _db=new StockContext();
            var q = _db.StockBaseInfos.FirstOrDefault();
            return q;
        }
    }
}
