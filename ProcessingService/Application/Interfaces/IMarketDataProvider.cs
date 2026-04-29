namespace ProcessingService.Application.Interfaces;

/// <summary>
/// PURPOSE: Provide market data and conditions for order execution
///
/// WHY CREATED:
/// - Abstraction for market data access (Dependency Inversion)
/// - Separates market logic from execution logic (Single Responsibility)
/// - Enables caching, fallback, and different data sources
/// - Testable: Mock market data in tests
/// - Repository Pattern: Abstract data access
///
/// SOLID PRINCIPLES APPLIED:
/// - Dependency Inversion (DIP): Executors depend on interface, not implementation
/// - Single Responsibility (SRP): Only provides market data, no execution logic
/// - Interface Segregation (ISP): Minimal, focused interface
///
/// REPOSITORY PATTERN:
/// This interface follows the repository pattern:
/// - Hides details of where data comes from (API, cache, database)
/// - Provides consistent interface regardless of source
/// - Easy to swap implementations (real vs mock, fast vs slow)
///
/// IMPLEMENTATIONS:
/// - RealMarketDataProvider: Fetches from live market data API
/// - CachedMarketDataProvider: Caches results for performance
/// - MockMarketDataProvider: Returns simulated data for testing
/// - FallbackMarketDataProvider: Falls back to alternative source if primary fails
///
/// USAGE:
/// var isHealthy = await marketData.IsMarketHealthyAsync(symbol);
/// var price = await marketData.GetCurrentPriceAsync(symbol, requestedPrice);
///
/// BENEFIT:
/// Can switch between different market data sources without touching execution code:
/// // In tests:
/// services.AddScoped<IMarketDataProvider, MockMarketDataProvider>();
/// // In production:
/// services.AddScoped<IMarketDataProvider, RealMarketDataProvider>();
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>
    /// Checks if market conditions are suitable for trading a symbol
    /// </summary>
    /// <param name="symbol">Instrument symbol (e.g., "AAPL")</param>
    /// <returns>True if market is open and healthy; false otherwise</returns>
    Task<bool> IsMarketHealthyAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets current market price for a symbol
    /// </summary>
    /// <param name="symbol">Instrument symbol</param>
    /// <param name="requestedPrice">Client's requested price (for reference/validation)</param>
    /// <returns>Current market price with variance</returns>
    Task<decimal> GetCurrentPriceAsync(string symbol, decimal requestedPrice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets market health status with reason
    /// </summary>
    /// <param name="symbol">Instrument symbol</param>
    /// <returns>Market status and reason</returns>
    Task<MarketHealthStatus> GetMarketStatusAsync(string symbol, CancellationToken cancellationToken = default);
}

/// <summary>
/// PURPOSE: Encapsulate market health information
///
/// WHY CREATED:
/// - Rich return type instead of just bool
/// - Carries reason for market being unhealthy
/// - Useful for logging and auditing
/// - Can extend with more market metadata
/// </summary>
public class MarketHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Reason { get; set; } = "Unknown";
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? AdditionalMetadata { get; set; }

    public static MarketHealthStatus Healthy(string reason = "Market is open") =>
        new() { IsHealthy = true, Reason = reason };

    public static MarketHealthStatus Unhealthy(string reason) =>
        new() { IsHealthy = false, Reason = reason };
}
