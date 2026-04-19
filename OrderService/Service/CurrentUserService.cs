using System.Security.Claims;
using Shared.Infrastructure.MediatRPipelines.Auth;

namespace OrderService.Service;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string UserId =>
        httpContextAccessor.HttpContext?.User?
            .FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public ClaimsPrincipal Principal =>
        httpContextAccessor.HttpContext?.User!;

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User?.IsInRole(role) ?? false;
}