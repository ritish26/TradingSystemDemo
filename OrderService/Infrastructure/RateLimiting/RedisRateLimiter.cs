using StackExchange.Redis;

namespace OrderService.Infrastructure.RateLimiting;

/// <summary>
/// Distributed rate limiter using Redis for horizontal scaling.
/// Per-instance in-memory limiters fail when requests go to different instances.
/// This implementation uses Redis to maintain shared state across all instances.
/// </summary>
public class RedisRateLimiter
{
    private readonly IDatabase _db;
    private readonly int _maxRequests;
    private readonly TimeSpan _window;

    public RedisRateLimiter(IConnectionMultiplexer redis, int maxRequests = 100, int windowSeconds = 60)
    {
        _db = redis.GetDatabase();
        _maxRequests = maxRequests;
        _window = TimeSpan.FromSeconds(windowSeconds);
    }

    public async Task<bool> IsAllowedAsync(string clientId)
    {
        var key = $"rate-limit:{clientId}";
        var count = await _db.StringGetAsync(key);

        if (count.IsNull)
        {
            await _db.StringSetAsync(key, 1, _window);
            return true;
        }

        var currentCount = int.Parse(count!);
        if (currentCount < _maxRequests)
        {
            await _db.StringIncrementAsync(key);
            return true;
        }

        return false;
    }
}
