using Shared.Domain.Events;

namespace ProcessingService.Application.Interfaces;

/// <summary>
/// PURPOSE: Publish order execution events to downstream systems
///
/// WHY CREATED:
/// - Abstraction for event publishing (Dependency Inversion)
/// - Enables asynchronous communication between services
/// - Decouples executor from specific messaging system
/// - Testable: Mock event publishing in tests
/// - Observer Pattern: Systems subscribe to execution events
///
/// SOLID PRINCIPLES APPLIED:
/// - Dependency Inversion (DIP): Depends on interface, not RabbitMQ directly
/// - Single Responsibility (SRP): Only publishes, doesn't execute or validate
/// - Open-Closed (OCP): Can add new publishers without modifying executor
///
/// STRATEGY PATTERN:
/// Different implementations can publish to:
/// - RabbitMQ
/// - Kafka
/// - Azure Service Bus
/// - Event Grid
/// - Database (Event Sourcing)
/// - In-memory (for testing)
///
/// OBSERVER PATTERN:
/// Executor publishes events:
/// - OrderExecutedEvent: Order successfully executed
/// - OrderExecutionFailedEvent: Order execution failed
///
/// Other services can subscribe:
/// - Notification service sends alerts
/// - Reporting service records execution
/// - Risk service updates risk metrics
/// - Compliance service audits trades
///
/// USAGE:
/// var result = await executor.ExecuteAsync(order);
/// if (result.IsSuccessful) {
///     await eventPublisher.PublishOrderExecutedAsync(
///         order.OrderId,
///         result.ExecutionId);
/// }
///
/// BENEFIT:
/// - Loose coupling: Executor doesn't know who consumes events
/// - Extensibility: Add new subscribers without changing executor
/// - Reliability: Events can be persisted and retried
/// - Auditability: Complete history of order executions
/// </summary>
public interface IOrderExecutionEventPublisher
{
    /// <summary>
    /// Publishes order executed event
    /// </summary>
    Task PublishOrderExecutedAsync(
        string orderId,
        string executionId,
        OrderPlacedEvent orderEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes order execution failed event
    /// </summary>
    Task PublishOrderExecutionFailedAsync(
        string orderId,
        string reason,
        OrderPlacedEvent orderEvent,
        CancellationToken cancellationToken = default);
}
