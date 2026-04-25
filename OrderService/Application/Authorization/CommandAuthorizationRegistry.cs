using OrderService.Application.Command;
using OrderService.Application.Queries;
using Shared.Application.Interfaces;
using Shared.Application.Pipelines.Auth;

namespace OrderService.AuthorizationRegistry;

public class CommandAuthorizationRegistry : ICommandAuthorizationRegistry
{
    private readonly Dictionary<Type, CommandAuthRule> _rules = new();
    private readonly ILogger<CommandAuthorizationRegistry> _logger;

    public CommandAuthorizationRegistry(ILogger<CommandAuthorizationRegistry> logger)
    {
        _logger = logger;

        _logger.LogInformation("[Registry] Building rules...");

        Register<OrderCreatedCommand>(new CommandAuthRule
        {
            Roles    = ["Admin"],
            Policies = ["CanCreateOrder"]
        });
        
        Register<GetOrdersQuery>(new CommandAuthRule
        {
            Roles    = ["ReadAdmin"],
            Policies = ["CanViewOrder"]
        });

        _logger.LogInformation("[Registry] Registered {Count} rules", _rules.Count);
    }

    private void Register<TCommand>(CommandAuthRule rule)
        => _rules[typeof(TCommand)] = rule;

    public CommandAuthRule? GetRule(Type commandType)
    {
        var rule = _rules.TryGetValue(commandType, out var r) ? r : null;

        _logger.LogInformation("[Registry] GetRule for {Command} → {Found}",
            commandType.Name, rule is not null ? "found" : "not found");

        return rule;
    }
}