namespace Authentication_Service.Infrastructure.Services;

public sealed class InMemoryUserRepository
{
    private static readonly Dictionary<string, (string Hash, string Role)> Users = new()
    {
        ["admin"] = (BCrypt.Net.BCrypt.HashPassword("admin123"), "Admin"),
        ["user"] = (BCrypt.Net.BCrypt.HashPassword("user123"), "ReadAdmin"),
    };

    public (string Hash, string Role)? Find(string username)
    {
        return Users.TryGetValue(username, out var user) ? user : null;
    }
}
