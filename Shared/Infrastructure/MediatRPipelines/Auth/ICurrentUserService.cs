using System.Security.Claims;

namespace Shared.Infrastructure.MediatRPipelines.Auth;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    string UserId       { get; }
    ClaimsPrincipal Principal { get; }
    bool IsInRole(string role);
}