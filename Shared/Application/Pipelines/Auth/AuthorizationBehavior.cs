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
            _logger.LogWarning("RULE NOT FOUND for {Request}", requestType.FullName);
            return await next();
        }

        _logger.LogInformation("Rule FOUND");
        
        if (rule.Roles != null && rule.Roles.Length > 0)
        {
            _logger.LogWarning("Required Roles: {Roles}", string.Join(",", rule.Roles));
        }
        else
        {
            _logger.LogWarning("No role requirement");
        }

        if (!_currentUser.IsAuthenticated)
        {
            _logger.LogError("AUTH FAILED: User not authenticated");
            throw new UnauthorizedAccessException();
        }
        
        if (rule.Roles?.Length > 0)
        {
            foreach (var role in rule.Roles)
            {
                var hasRole = _currentUser.IsInRole(role);

                _logger.LogInformation(
                    "Role Check → Required: {Role}, HasRole: {Result}",
                    role,
                    hasRole);
            }

            var isAuthorized = rule.Roles.Any(r => _currentUser.IsInRole(r));

            if (!isAuthorized)
            {
                _logger.LogError("AUTH FAILED: Role mismatch");
                throw new ForbiddenException();
            }
        }
        
        if (rule.Policies?.Length > 0)
        {
            foreach (var policy in rule.Policies)
            {
                var result = await _authService
                    .AuthorizeAsync(_currentUser.Principal, policy);

                if (!result.Succeeded)
                {
                    throw new ForbiddenException();
                }
            }
        }
        return await next();
    }
}