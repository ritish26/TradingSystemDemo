using RabbitMQ.Client;

namespace Shared.Infrastructure.Helper;

/// <summary>
/// CorrelationIdExtensions - Helper methods for working with correlation IDs
/// in RabbitMQ messages and HTTP clients
/// </summary>
public static class CorrelationIdExtensions
{
    /// <summary>
    /// Enriches RabbitMQ message properties with correlation ID
    /// </summary>
    public static void SetCorrelationId(this IBasicProperties properties)
    {
        var correlationId = CorrelationIdContext.GetCorrelationId();
        properties.Headers ??= new Dictionary<string, object>();
        properties.Headers[Constant.Constant.CorrelationIdHeaderName] = 
            System.Text.Encoding.UTF8.GetBytes(correlationId);
    }

    /// <summary>
    /// Extracts correlation ID from RabbitMQ message properties
    /// </summary>
    public static string GetCorrelationId(this IBasicProperties properties)
    {
        if (properties?.Headers != null && 
            properties.Headers.TryGetValue(Constant.Constant.CorrelationIdHeaderName, out var headerValue))
        {
            if (headerValue is byte[] bytes)
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}

