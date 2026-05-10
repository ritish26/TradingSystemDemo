using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Authentication_Service.Infrastructure.Services;

/// <summary>
/// Generates cache keys for JWT tokens by hashing username+role+permissions.
/// SHA256 ensures consistent length and provides cache busting when claims change.
/// Permissions are sorted before hashing to ensure identical hashes for identical claim sets
/// regardless of the order they appear in the claims list.
/// </summary>
public static class TokenCacheKeyGenerator
{
    /// <summary>
    /// Generate a SHA256-based cache key from user identity and claims.
    /// Different users always have different keys (username included).
    /// Same user with different permissions has different key (permissions hashed).
    /// Same user with same permissions always produces the same key (consistent hashing).
    /// </summary>
    public static string GenerateKey(string username, string role, IEnumerable<Claim> claims)
    {
        var permissions = claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .OrderBy(p => p)
            .ToList();

        var input = $"{username}:{role}:{string.Join(",", permissions)}";

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
