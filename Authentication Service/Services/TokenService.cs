using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Authentication_Service.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authentication_Service.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(string username)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, username),
            new("permission", "trade:view")
        ];

        if (username == "admin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim("permission", "trade:create"));
            claims.Add(new Claim("permission", "order:create"));
        }
        
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "ReadAdmin"));
            claims.Add(new Claim(ClaimTypes.Role, "User"));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtSettings.Secret));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}