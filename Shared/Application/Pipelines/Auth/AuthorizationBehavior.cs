using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Shared.Application.Interfaces;
using Shared.Domain.Exceptions;

namespace Shared.Application.Pipelines.Auth;

public class AuthorizationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<AuthorizationBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthorizationService _authService;
    private readonly ICommandAuthorizationRegistry _registry;

    public AuthorizationBehavior(
        ICurrentUserService currentUser,
        IAuthorizationService authService,
        ICommandAuthorizationRegistry registry,
        ILogger<AuthorizationBehavior<TRequest, TResponse>> logger)
    {
        _currentUser = currentUser;
        _authService = authService;
        _registry = registry;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestType = typeof(TRequest);
        var rule = _registry.GetRule(requestType);

        if (rule == null)
        {
            return await next();
        }

        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogError("Unauthorized: User not authenticated for {Command}", requestType.Name);
            throw new UnauthorizedAccessException();
        }

        // Check policies if required
        if (rule.Policies?.Length > 0)
        {
            foreach (var policy in rule.Policies)
            {
                var result = await _authService.AuthorizeAsync(_currentUser.Principal, policy);

                if (!result.Succeeded)
                {
                    _logger.LogError("Forbidden: Policy '{Policy}' check failed for {Command}", policy, requestType.Name);
                    throw new ForbiddenException();
                }
            }
        }

        return await next();
    }
}
