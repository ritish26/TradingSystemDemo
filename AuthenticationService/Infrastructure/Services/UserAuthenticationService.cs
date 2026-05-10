using AuthenticationService.Application.Interfaces;
using AuthenticationService.Application.Models;

namespace AuthenticationService.Infrastructure.Services;

/// <summary>
/// Authenticates users against stored credentials using BCrypt.
/// BCrypt is preferred over SHA-256 for password hashing because:
/// - It includes a per-user salt, preventing rainbow table attacks
/// - It's deliberately slow via a cost factor, protecting against brute force attacks
/// - It's designed specifically for passwords, unlike SHA-256 which is a general-purpose hash function
/// </summary>
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

        if (!BCrypt.Net.BCrypt.Verify(password, user.Value.Hash))
            return null;

        return new AuthenticatedUser { Username = username, Role = user.Value.Role };
    }
}
