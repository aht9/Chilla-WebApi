using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Chilla.Infrastructure.Authentication;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string username, IEnumerable<string> roles);
    string GenerateRefreshToken();
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // دقت کنید ورودی سوم را به IEnumerable<string> تغییر دادیم
    public string GenerateAccessToken(Guid userId, string username, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            // اگر Username نال بود، رشته خالی بگذارید تا توکن نشکند
            new Claim(JwtRegisteredClaimNames.UniqueName, username ?? string.Empty), 
            // Jti یک شناسه یکتا برای خود توکن است که برای باطل کردن (Revoke) کاربرد دارد
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        // اضافه کردن تمام نقش‌های کاربر به توکن
        if (roles != null)
        {
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]));
        // الگوریتم HmacSha256 کاملاً امن و استاندارد است
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["JwtSettings:Issuer"],
            audience: _configuration["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15), // Access Token حتماً باید عمر کوتاهی داشته باشد
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // رفرش توکن صرفاً یک رشته رندوم امن است
        var randomNumber = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
}