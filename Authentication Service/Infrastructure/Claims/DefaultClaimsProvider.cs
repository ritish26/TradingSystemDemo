using System.Security.Claims;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Application.Models;

namespace Authentication_Service.Infrastructure.Claims;

public sealed class DefaultClaimsProvider : IUserClaimsProvider
{
    public bool CanHandle(string role) => role == "ReadAdmin";

    public IEnumerable<Claim> GetClaims(AuthenticatedUser user)
    {
        yield return new Claim(ClaimTypes.Name, user.Username);
        yield return new Claim(ClaimTypes.Role, "ReadAdmin");
        yield return new Claim("permission", "order:read");
    }
}
