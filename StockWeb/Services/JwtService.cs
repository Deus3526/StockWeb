using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace StockWeb.Services
{
    public class JwtService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ConcurrentDictionary<string, RefreshToken> _refreshTokens = new ConcurrentDictionary<string, RefreshToken>();
        public JwtService(JwtSettings jwtSettings)
        {
            _jwtSettings = jwtSettings;
        }
        public string GenerateToken(string userId)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, userId) }),
                Expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiredTime),
                Issuer = _jwtSettings.Issuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        public RefreshToken GenerateRefreshToken(string userId)
        {
            return new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expiration = DateTime.UtcNow.AddHours(_jwtSettings.RefreshTokenExpiredTime),  // 設定 refresh token 的過期時間
                UserId = userId
            };
        }
        public RefreshToken? GetRefreshToken(string refreshToken)
        {
            _refreshTokens.TryGetValue(refreshToken, out var token);
            return token;
        }
        public void AddRefreshToken(RefreshToken refreshToken)
        {
            _refreshTokens.TryAdd(refreshToken.Token, refreshToken);
        }
        public void RemoveRefreshToken(string? refreshToken)
        {
            if(refreshToken == null) return;
            _refreshTokens.TryRemove(refreshToken, out _);
        }
        public bool ValidateRefreshToken(RefreshToken? refreshToken)
        {
            if (refreshToken == null || refreshToken.Expiration < DateTime.Now)
            {
                return false;
            }
            return true;
        }

    }
    public class JwtSettings
    {
        public required  string Key { get; set; }
        public required  string Issuer { get; set; }
        public int ExpiredTime { get; set; }
        public  int RefreshTokenExpiredTime { get; set; }
    }
    public class RefreshToken
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public required string UserId { get; set; }
        // 你也可能需要添加其他字段，例如用於識別用戶的 ClientId 等
    }
}
