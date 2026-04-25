namespace Shared.Domain.Constants;

public static class Constants
{
        public static readonly string OrderPlacedEventQueueName = "order-placed-events";
        public static readonly string Placed = "PLACED";
        public static readonly string Pending = "Pending";
        public static readonly string CorrelationIdHeaderName = "X-Correlation-Id";
}