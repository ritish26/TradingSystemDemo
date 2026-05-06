using System.Security.Cryptography;
using System.Text.Json;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Application.Models;
using Authentication_Service.Configuration;
using Microsoft.Extensions.Options;

namespace Authentication_Service.Infrastructure.Services;

public sealed class TokenService : ITokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly ITransitSigner _transitSigner;
    private readonly IEnumerable<IUserClaimsProvider> _claimsProviders;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        ITransitSigner transitSigner,
        IEnumerable<IUserClaimsProvider> claimsProviders)
    {
        _jwtSettings = jwtSettings.Value;
        _transitSigner = transitSigner;
        _claimsProviders = claimsProviders;
    }

    public string GenerateToken(AuthenticatedUser user)
    {
        var claimsProvider = _claimsProviders.FirstOrDefault(p => p.CanHandle(user.Role))
                             ?? throw new InvalidOperationException(
                                 $"No claims provider registered for role '{user.Role}'");
        var claims = claimsProvider.GetClaims(user).ToList();

        var keyVersion = _transitSigner.GetCurrentKeyVersionAsync().GetAwaiter().GetResult();
        var header = BuildHeader(keyVersion);
        var payload = BuildPayload(claims);

        var headerB64 = Base64UrlEncode(header);
        var payloadB64 = Base64UrlEncode(payload);
        var signingInput = $"{headerB64}.{payloadB64}";

        var signingResult = _transitSigner.SignAsync(signingInput).GetAwaiter().GetResult();

        return $"{signingInput}.{signingResult.Signature}";
    }

    private string BuildHeader(int keyVersion)
    {
        var header = new
        {
            alg = "RS256",
            typ = "JWT",
            kid = keyVersion.ToString()
        };

        return JsonSerializer.Serialize(header);
    }

    private string BuildPayload(List<System.Security.Claims.Claim> claims)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddMinutes(_jwtSettings.TokenExpiryMinutes);

        var payload = new Dictionary<string, object>
        {
            { "iss", _jwtSettings.Issuer },
            { "aud", _jwtSettings.Audience },
            { "iat", new DateTimeOffset(now).ToUnixTimeSeconds() },
            { "exp", new DateTimeOffset(expiresAt).ToUnixTimeSeconds() }
        };

        foreach (var claim in claims)
        {
            if (!payload.ContainsKey(claim.Type))
            {
                payload[claim.Type] = claim.Value;
            }
        }

        return JsonSerializer.Serialize(payload);
    }

    private static string Base64UrlEncode(string input)
    {
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(inputBytes);
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}