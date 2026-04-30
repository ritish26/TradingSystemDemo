namespace Shared.Application.Common;

public static class CorrelationIdContext
{
    private static readonly AsyncLocal<string> CorrelationId = new(); // one value per request, survives await

    public static string GetCorrelationId()
        => CorrelationId.Value ?? Guid.NewGuid().ToString("N");

    public static void SetCorrelationId(string correlationId)
        => CorrelationId.Value = correlationId;  

    public static void Clear()
        => CorrelationId.Value = null;
}