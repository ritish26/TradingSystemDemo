namespace AuthenticationService.Application.Interfaces;

/// <summary>
/// Caches JWT tokens in Redis to reduce Vault calls.
/// Cache key is a SHA256 hash of username+role+permissions — when claims change, the hash changes,
/// automatically causing a cache miss and forcing a fresh token from Vault.
/// </summary>
public interface ITokenCacheService
{
    /// <summary>
    /// Retrieve cached token if it exists and has sufficient TTL remaining.
    /// Returns null if cache miss OR if cached token has less than CacheRefreshThresholdMinutes remaining.
    /// </summary>
    Task<string?> GetCachedTokenAsync(string cacheKey);

    /// <summary>
    /// Store a token in cache with a TTL in seconds (aligned to JWT expiry).
    /// Overwrites existing token if present.
    /// </summary>
    Task SetCachedTokenAsync(string cacheKey, string token, int ttlSeconds);

    /// <summary>
    /// Invalidate a cached token (used when user permissions change or admin revokes access).
    /// Safe to call even if the key does not exist.
    /// </summary>
    Task InvalidateAsync(string cacheKey);
}
