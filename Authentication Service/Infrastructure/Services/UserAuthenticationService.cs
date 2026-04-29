using System.Security.Cryptography;
using System.Text;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Application.Models;

namespace Authentication_Service.Infrastructure.Services;

public sealed class UserAuthenticationService : IUserAuthenticationService
{
    private readonly InMemoryUserRepository _repository;

    public UserAuthenticationService(InMemoryUserRepository repository)
    {
        _repository = repository;
    }

    public AuthenticatedUser? Authenticate(string username, string password)
    {
        var user = _repository.Find(username);
        if (user is null) return null;

        var inputHash = Sha256Hash(password);
        if (inputHash != user.Value.Hash) return null;

        return new AuthenticatedUser { Username = username, Role = user.Value.Role };
    }

    private static string Sha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(hashedBytes);
    }
}
