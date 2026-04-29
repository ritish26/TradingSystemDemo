using RabbitMQ.Client;

namespace Shared.Application.Interfaces;

// Abstraction for messaging connections. Allows switching between RabbitMQ, Azure Service Bus, etc.
// Follows DIP - services depend on abstraction, not concrete messaging implementation details.
public interface IMessagingConnection
{
    IModel CreateChannel();
    void Close();
}
