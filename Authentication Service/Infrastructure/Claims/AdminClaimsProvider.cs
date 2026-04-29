using System.Security.Claims;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Application.Models;

namespace Authentication_Service.Infrastructure.Claims;

public sealed class AdminClaimsProvider : IUserClaimsProvider
{
    public bool CanHandle(string role) => role == "Admin";

    public IEnumerable<Claim> GetClaims(AuthenticatedUser user)
    {
        yield return new Claim(ClaimTypes.Name, user.Username);
        yield return new Claim(ClaimTypes.Role, "Admin");
        yield return new Claim("permission", "order:create");
        yield return new Claim("permission", "order:read");
    }
}
