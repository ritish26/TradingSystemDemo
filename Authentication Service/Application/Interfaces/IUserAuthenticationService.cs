using Authentication_Service.Application.Models;

namespace Authentication_Service.Application.Interfaces;

public interface IUserAuthenticationService
{
    AuthenticatedUser? Authenticate(string username, string password);
}
