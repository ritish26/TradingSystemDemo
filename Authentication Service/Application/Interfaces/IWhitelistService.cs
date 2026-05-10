namespace Authentication_Service.Application.Interfaces;

/// <summary>
/// Manages IP whitelist for rate limiting bypass.
/// Whitelisted IPs (FNZ office, VPN, trusted partners) skip rate limiting entirely.
/// Stored in Redis SET for dynamic management without redeploy.
/// </summary>
public interface IWhitelistService
{
    /// <summary>
    /// Check if IP is whitelisted (trusted source).
    /// </summary>
    Task<bool> IsWhitelistedAsync(string ipAddress);

    /// <summary>
    /// Add IP to whitelist. Can be called at runtime.
    /// </summary>
    Task AddToWhitelistAsync(string ipAddress);

    /// <summary>
    /// Remove IP from whitelist.
    /// </summary>
    Task RemoveFromWhitelistAsync(string ipAddress);

    /// <summary>
    /// Get all whitelisted IPs.
    /// </summary>
    Task<IEnumerable<string>> GetAllWhitelistedIpsAsync();
}
