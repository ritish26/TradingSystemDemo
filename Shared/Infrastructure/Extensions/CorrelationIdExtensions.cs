using RabbitMQ.Client;
using Shared.Application.Common;
using Constants = Shared.Domain.Constants.Constants;

namespace Shared.Infrastructure.Extensions;

/// <summary>
/// CorrelationIdExtensions - Extensions methods for working with correlation IDs
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
        properties.Headers[Constants.CorrelationIdHeaderName] = 
            System.Text.Encoding.UTF8.GetBytes(correlationId);
    }

    /// <summary>
    /// Extracts correlation ID from RabbitMQ message properties
    /// </summary>
    public static string GetCorrelationId(this IBasicProperties properties)
    {
        if (properties?.Headers != null && 
            properties.Headers.TryGetValue(Constants.CorrelationIdHeaderName, out var headerValue))
        {
            if (headerValue is byte[] bytes)
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}

