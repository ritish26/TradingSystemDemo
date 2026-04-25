using System.Security.Claims;

namespace Shared.Application.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string UserId       { get; }
    ClaimsPrincipal Principal { get; }
    bool IsInRole(string role);
}