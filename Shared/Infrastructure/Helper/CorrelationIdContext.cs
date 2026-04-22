namespace Shared.Infrastructure.Helper;

/// <summary>
/// CorrelationIdContext - Stores and retrieves the correlation ID for request tracking
/// across microservices. Uses AsyncLocal for async context preservation.
/// </summary>
public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string> _correlationId = new();

    /// <summary>
    /// Gets the current correlation ID for this async context
    /// </summary>
    public static string GetCorrelationId()
    {
        return _correlationId.Value ?? Guid.NewGuid().ToString("N");
    }

    /// <summary>
    /// Sets the correlation ID for this async context
    /// </summary>
    public static void SetCorrelationId(string correlationId)
    {
        _correlationId.Value = string.IsNullOrWhiteSpace(correlationId) 
            ? Guid.NewGuid().ToString("N") 
            : correlationId;
    }

    /// <summary>
    /// Clears the correlation ID from this async context
    /// </summary>
    public static void Clear()
    {
        _correlationId.Value = null;
    }
}

