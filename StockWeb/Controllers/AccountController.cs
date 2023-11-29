
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockWeb.DbModels;
using StockWeb.Models.RequestParms;
using StockWeb.Services;
using StockWeb.StaticData;

namespace StockWeb.Controllers
{
   
    [Route("api/[controller]/[action]")]
    [ApiController]
    [ApiExplorerSettings(GroupName = nameof(ApiGroups.Account))]
    [Tags("登入相關的端點")]
    public class AccountController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly StockContext _db;
        public AccountController(JwtService jwtService,StockContext db) 
        {
            _jwtService = jwtService;
            _db = db;
        }

        [HttpPost]
        public ActionResult<StockBaseInfo> testNeedLogin()
        {
            var q=_db.StockBaseInfos.First();
            return q;
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult<StockBaseInfo> testNoNeedLogin()
        {
            var q = _db.StockBaseInfos.First();
            return q;
        }

        /// <summary>
        /// 登入取token
        /// </summary>
        /// <param name="parm"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async  Task<ActionResult> Login(LoginParm parm)
        {
            var q = await _db.Users.Where(u => u.Account == parm.account).FirstOrDefaultAsync();
            if(q==null) return Unauthorized();
            if(BCrypt.Net.BCrypt.Verify(parm.password,q.Password)==false) return Unauthorized();


            string userId=q.UserId.ToString();
            string token = _jwtService.GenerateToken(userId);
            RefreshToken refreshToken = _jwtService.GenerateRefreshToken(userId);
            _jwtService.AddRefreshToken(refreshToken);
            return Ok(new
            {
                accessToken =token,
                refreshToken = refreshToken.Token
            });

        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult> RegisterAccount(RegisterAccountParm parm)
        {
            if(await _db.Users.AnyAsync(u=>u.Account==parm.account))
            {
                return Conflict();
            }
            User user = new User
            {
                UserId=new Guid(),
                Account=parm.account,
                UserName=parm.userName,
                Password= BCrypt.Net.BCrypt.HashPassword(parm.password)
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}
