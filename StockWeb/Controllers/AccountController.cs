using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StockWeb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        [HttpPost]
        public string test()
        {
            return "test";
        }
    }
}
