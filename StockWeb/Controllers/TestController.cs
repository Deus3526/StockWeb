using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StockWeb.DbModels;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public ActionResult<StockBaseInfo> test()
        {
            StockContext _db=new StockContext();
            var q = _db.StockBaseInfos.FirstOrDefault();
            return q;
        }
    }
}
