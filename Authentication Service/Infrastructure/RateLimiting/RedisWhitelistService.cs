using Authentication_Service.Application.Interfaces;
using StackExchange.Redis;

namespace Authentication_Service.Infrastructure.RateLimiting;

/// <summary>
/// Redis-based IP whitelist implementation.
/// Uses Redis SET for O(1) lookup and O(1) add/remove.
/// Allows dynamic whitelist changes without redeploying services.
/// </summary>
public class RedisWhitelistService : IWhitelistService
{
    private readonly IDatabase _db;
    private const string WhitelistKey = "ip-whitelist";

    public RedisWhitelistService(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    /// <summary>
    /// Check if IP is in whitelist.
    /// Redis SISMEMBER: O(1) operation, very fast.
    /// </summary>
    public async Task<bool> IsWhitelistedAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return false;

        return await _db.SetContainsAsync(WhitelistKey, ipAddress);
    }

    /// <summary>
    /// Add IP to whitelist.
    /// Redis SADD: idempotent, safe to call multiple times.
    /// </summary>
    public async Task AddToWhitelistAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return;

        await _db.SetAddAsync(WhitelistKey, ipAddress);
    }

    /// <summary>
    /// Remove IP from whitelist.
    /// Redis SREM: idempotent, safe if IP not in set.
    /// </summary>
    public async Task RemoveFromWhitelistAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            return;

        await _db.SetRemoveAsync(WhitelistKey, ipAddress);
    }

    /// <summary>
    /// Get all whitelisted IPs.
    /// Redis SMEMBERS: returns entire set.
    /// </summary>
    public async Task<IEnumerable<string>> GetAllWhitelistedIpsAsync()
    {
        var members = await _db.SetMembersAsync(WhitelistKey);
        return members.Select(m => m.ToString()).ToList();
    }
}
