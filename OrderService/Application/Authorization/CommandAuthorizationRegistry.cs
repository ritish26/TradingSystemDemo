using OrderService.Application.Command;
using OrderService.Application.Queries;
using Shared.Application.Interfaces;
using Shared.Application.Pipelines.Auth;

namespace OrderService.Application.Authorization;

public class CommandAuthorizationRegistry : ICommandAuthorizationRegistry
{
    private readonly Dictionary<Type, CommandAuthRule> _rules = new();

    public CommandAuthorizationRegistry()
    {
        Register<OrderCreatedCommand>(new CommandAuthRule
        {
            Policies = ["CanCreateOrder"]
        });

        Register<GetOrdersQuery>(new CommandAuthRule
        {
            Policies = ["CanViewOrder"]
        });
    }

    private void Register<TCommand>(CommandAuthRule rule)
        => _rules[typeof(TCommand)] = rule;

    public CommandAuthRule? GetRule(Type commandType)
        => _rules.TryGetValue(commandType, out var rule) ? rule : null;
}