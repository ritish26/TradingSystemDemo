using StackExchange.Redis;

namespace Authentication_Service.Infrastructure.RateLimiting;

/// <summary>
/// Distributed rate limiter using Redis for horizontal scaling.
/// Shared state across all Authentication Service instances.
/// Supports dual-key rate limiting: per-IP and per-username with different limits.
/// </summary>
public class RedisRateLimiter
{
    private readonly IDatabase _db;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RedisRateLimiter(IConnectionMultiplexer redis, int maxRequests = 5, int windowSeconds = 300)
    {
        _db = redis.GetDatabase();
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    /// <summary>
    /// Check if key is allowed using default limits.
    /// Used for per-username rate limiting.
    /// </summary>
    public async Task<bool> IsAllowedAsync(string key)
    {
        return await IsAllowedAsync(key, _maxRequests, (int)_window.TotalSeconds);
    }

    /// <summary>
    /// Check if key is allowed using custom limits.
    /// Allows different limits for IP vs username:
    /// - IP limit: 20 per 300s (lenient — NAT/office)
    /// - Username limit: 5 per 300s (strict — brute force)
    /// </summary>
    public async Task<bool> IsAllowedAsync(string key, int maxRequests, int windowSeconds)
    {
        var count = await _db.StringGetAsync(key);

        if (count.IsNull)
        {
            await _db.StringSetAsync(key, 1, TimeSpan.FromSeconds(windowSeconds));
            return true;
        }

        var currentCount = int.Parse(count!);
        if (currentCount < maxRequests)
        {
            await _db.StringIncrementAsync(key);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Get TTL (seconds until expiration) for a rate limit key.
    /// Used to calculate Retry-After header.
    /// </summary>
    public async Task<int> GetTtlAsync(string key)
    {
        var ttl = await _db.KeyTimeToLiveAsync(key);
        return (int)(ttl?.TotalSeconds ?? _window.TotalSeconds);
    }
}
