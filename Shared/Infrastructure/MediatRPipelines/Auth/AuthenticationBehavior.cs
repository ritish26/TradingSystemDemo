using MediatR;
using Microsoft.AspNetCore.Authorization;


using Shared.Infrastructure.MediatRPipelines.Auth;

public class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authService;
    private readonly ICommandAuthorizationRegistry _registry;

    public AuthorizationBehavior(
        ICurrentUserService currentUser,
        IAuthorizationService authService,
        ICommandAuthorizationRegistry registry)
    {
        _currentUser = currentUser;
        _authService = authService;
        _registry    = registry;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var rule = _registry.GetRule(typeof(TRequest));
    
        if (rule is null)
            return await next();

        if (!_currentUser.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated.");

        if (rule.Roles?.Length > 0)
        {
            var hasRole = rule.Roles.Any(r => _currentUser.IsInRole(r));
            if (!hasRole)
                throw new UnauthorizedAccessException("Insufficient role.");
        }

        if (rule.Policies?.Length > 0)
        {
            foreach (var policy in rule.Policies)
            {
                var result = await _authService
                    .AuthorizeAsync(_currentUser.Principal, policy);

                if (!result.Succeeded)
                    throw new UnauthorizedAccessException($"Policy '{policy}' failed.");
            }
        }

        return await next();
    }
}