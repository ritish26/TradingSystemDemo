using System.Security.Claims;
using AuthenticationService.Application.Models;

namespace AuthenticationService.Application.Interfaces;

public interface IUserClaimsProvider
{
    bool CanHandle(string role);
    IEnumerable<Claim> GetClaims(AuthenticatedUser user);
}
