using System.Security.Cryptography;
using System.Text;

namespace Authentication_Service.Infrastructure.Services;

public sealed class InMemoryUserRepository
{
    private static readonly Dictionary<string, (string Hash, string Role)> Users = new()
    {
        ["admin"] = (Sha256Hash("admin123"), "Admin"),
        ["user"] = (Sha256Hash("user123"), "ReadAdmin"),
    };

    public (string Hash, string Role)? Find(string username)
    {
        return Users.TryGetValue(username, out var user) ? user : null;
    }

    private static string Sha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashedBytes);
    }
}
