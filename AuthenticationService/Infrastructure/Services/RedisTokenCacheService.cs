using AuthenticationService.Application.Interfaces;
using AuthenticationService.Configuration;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace AuthenticationService.Infrastructure.Services;

/// <summary>
/// Caches JWT tokens in Redis to reduce Vault Transit calls.
/// Fail-open design: if Redis is unavailable, tokens are generated fresh from Vault without impacting login.
/// TTL check: cached tokens with less than CacheRefreshThresholdMinutes remaining are treated as cache misses.
/// </summary>
public sealed class RedisTokenCacheService : ITokenCacheService
{
    private readonly IDatabase _db;
    private readonly int _cacheRefreshThresholdSeconds;
    private const string KeyPrefix = "token-cache:";

    public RedisTokenCacheService(
        IConnectionMultiplexer redis,
        IOptions<JwtSettings> jwtSettings)
    {
        _db = redis.GetDatabase();
        _cacheRefreshThresholdSeconds = jwtSettings.Value.CacheRefreshThresholdMinutes * 60;
    }

    /// <summary>
    /// Retrieve cached token if present and has sufficient TTL.
    /// Returns null if cache miss, if token is expired, or if TTL is below the refresh threshold.
    /// Wraps Redis operations with try/catch to fail open (null return on any Redis error).
    /// </summary>
    public async Task<string?> GetCachedTokenAsync(string cacheKey)
    {
        try
        {
            var key = $"{KeyPrefix}{cacheKey}";
            var cachedToken = await _db.StringGetAsync(key);

            if (!cachedToken.HasValue)
                return null;

            var ttl = await _db.KeyTimeToLiveAsync(key);

            if (!ttl.HasValue || ttl.Value.TotalSeconds < _cacheRefreshThresholdSeconds)
                return null;

            return cachedToken.ToString();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Store a token in Redis with a TTL in seconds (typically aligned to JWT expiry).
    /// Overwrites existing token if present.
    /// Wraps Redis operations with try/catch to fail open (silent on any Redis error).
    /// </summary>
    public async Task SetCachedTokenAsync(string cacheKey, string token, int ttlSeconds)
    {
        try
        {
            var key = $"{KeyPrefix}{cacheKey}";
            await _db.StringSetAsync(key, token, TimeSpan.FromSeconds(ttlSeconds));
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Invalidate a cached token (used when permissions change or session is revoked).
    /// Safe to call even if the key does not exist.
    /// Wraps Redis operations with try/catch to fail open (silent on any Redis error).
    /// </summary>
    public async Task InvalidateAsync(string cacheKey)
    {
        try
        {
            var key = $"{KeyPrefix}{cacheKey}";
            await _db.KeyDeleteAsync(key);
        }
        catch
        {
            // ignored
        }
    }
}
