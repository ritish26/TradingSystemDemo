using System.IdentityModel.Tokens.Jwt;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Application.Models;
using Authentication_Service.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authentication_Service.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly IRsaKeyProvider _rsaKeyProvider;
    private readonly IEnumerable<IUserClaimsProvider> _claimsProviders;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        IRsaKeyProvider rsaKeyProvider,
        IEnumerable<IUserClaimsProvider> claimsProviders)
    {
        _jwtSettings = jwtSettings.Value;
        _rsaKeyProvider = rsaKeyProvider;
        _claimsProviders = claimsProviders;
    }

    public string GenerateToken(AuthenticatedUser user)
    {
        var claimsProvider = _claimsProviders.First(p => p.CanHandle(user.Role));
        var claims = claimsProvider.GetClaims(user).ToList();

        var rsa = _rsaKeyProvider.GetPrivateKey();
        var securityKey = new RsaSecurityKey(rsa) { KeyId = "key-1" };
        var creds = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.TokenExpiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}