using System.Security.Claims;
using Authentication_Service.Application.Models;

namespace Authentication_Service.Application.Interfaces;

public interface IUserClaimsProvider
{
    bool CanHandle(string role);
    IEnumerable<Claim> GetClaims(AuthenticatedUser user);
}
