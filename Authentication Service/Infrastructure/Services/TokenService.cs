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
    private readonly ITokenCacheService _tokenCacheService;

    public TokenService(
        IOptions<JwtSettings> jwtSettings,
        ITransitSigner transitSigner,
        IEnumerable<IUserClaimsProvider> claimsProviders,
        ITokenCacheService tokenCacheService)
    {
        _jwtSettings = jwtSettings.Value;
        _transitSigner = transitSigner;
        _claimsProviders = claimsProviders;
        _tokenCacheService = tokenCacheService;
    }

    public async Task<string> GenerateTokenAsync(AuthenticatedUser user)
    {
        var claimsProvider = _claimsProviders.FirstOrDefault(p => p.CanHandle(user.Role))
                             ?? throw new InvalidOperationException(
                                 $"No claims provider registered for role '{user.Role}'");
        var claims = claimsProvider.GetClaims(user).ToList();

        // Generate cache key from user identity and claims
        var cacheKey = TokenCacheKeyGenerator.GenerateKey(user.Username, user.Role, claims);

        // Check Redis cache (fail-open: if Redis is down, we just generate fresh token)
        string? cachedToken = null;
        try
        {
            cachedToken = await _tokenCacheService.GetCachedTokenAsync(cacheKey);
        }
        catch
        {
            // ignored
        }

        if (cachedToken != null)
            return cachedToken;

        // Cache miss — generate fresh JWT from Vault
        var keyVersion = await _transitSigner.GetCurrentKeyVersionAsync();
        var header = BuildHeader(keyVersion);
        var payload = BuildPayload(claims);

        var headerB64 = Base64UrlEncode(header);
        var payloadB64 = Base64UrlEncode(payload);
        var signingInput = $"{headerB64}.{payloadB64}";

        var signingResult = await _transitSigner.SignAsync(signingInput);
        var token = $"{signingInput}.{signingResult.Signature}";

        // Store in cache (fail-open: if Redis is down, we still return valid token)
        try
        {
            await _tokenCacheService.SetCachedTokenAsync(
                cacheKey,
                token,
                _jwtSettings.TokenExpiryMinutes * 60);
        }
        catch
        {
            // ignored
        }

        return token;
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