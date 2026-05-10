using AuthenticationService.Application.Models;

namespace AuthenticationService.Application.Interfaces;

public interface IUserAuthenticationService
{
    AuthenticatedUser? Authenticate(string username, string password);
}
