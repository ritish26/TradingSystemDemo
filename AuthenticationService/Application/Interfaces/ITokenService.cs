using AuthenticationService.Application.Models;

namespace AuthenticationService.Application.Interfaces;

public interface ITokenService
{
    Task<string> GenerateTokenAsync(AuthenticatedUser user);
}