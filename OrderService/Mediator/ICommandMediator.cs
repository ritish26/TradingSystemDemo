namespace OrderService2.Mediator;

public interface ICommandMediator
{
    Task SendAsync<TCommand>(TCommand command) where TCommand : class;
}

