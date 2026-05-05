using Authentication_Service.Application.Models;

namespace Authentication_Service.Application.Interfaces;

public interface ITokenService
{
    string GenerateToken(AuthenticatedUser user);
}