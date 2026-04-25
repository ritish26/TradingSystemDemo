using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authentication_Service.Infrastructure.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;

    public TokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
    }

    public string GenerateToken(string username)
    {
        // 1. Claims 
        List<Claim> claims =
        [
            new(ClaimTypes.Name, username),
        ];

        if (username == "admin")
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim("permission", "order:create"));
            claims.Add(new Claim("permission", "order:read"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "ReadAdmin"));
            claims.Add(new Claim("permission", "order:read"));
        }

        // 2. Load PRIVATE key from PEM file
        var privateKeyPem = File.ReadAllText(
            Path.Combine(Directory.GetCurrentDirectory(), _jwtSettings.PrivateKeyPath));

        var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem.ToCharArray());

        var securityKey = new RsaSecurityKey(rsa)
        {
            KeyId = "key-1" // optional but recommended (for rotation)
        };

        var creds = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.RsaSha256 
        );

        // 3. Create token (same as before)
        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        // 4. Return JWT
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}