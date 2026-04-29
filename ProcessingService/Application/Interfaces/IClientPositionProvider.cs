namespace ProcessingService.Application.Interfaces;

/// <summary>
/// PURPOSE: Provide client position data for order execution validation
///
/// WHY CREATED:
/// - Abstraction for client position access (Dependency Inversion)
/// - Separates position check logic from execution logic (Single Responsibility)
/// - Enables different data sources (real portfolio, simulated, test data)
/// - Testable: Mock positions in tests
/// - Repository Pattern: Abstract data access
///
/// SOLID PRINCIPLES APPLIED:
/// - Dependency Inversion (DIP): Executors depend on interface
/// - Single Responsibility (SRP): Only provides position data
/// - Interface Segregation (ISP): Minimal, focused interface
///
/// REPOSITORY PATTERN:
/// This interface follows the repository pattern:
/// - Hides where position data comes from (database, cache, service)
/// - Provides consistent interface regardless of source
/// - Easy to mock for testing
///
/// IMPLEMENTATIONS:
/// - DatabasePositionProvider: Queries real portfolio database
/// - CachedPositionProvider: Caches position data for performance
/// - MockPositionProvider: Returns simulated positions for testing
/// - PortfolioServiceClient: Calls external portfolio service
///
/// USAGE:
/// var hasPosition = await positions.HasSufficientPositionAsync(clientId, symbol, quantity);
/// if (!hasPosition) {
///     throw OrderExecutionException.InsufficientPosition(orderId, clientId, symbol);
/// }
///
/// BENEFIT:
/// - Different clients may have position data in different systems
/// - Can switch between sources without touching execution code
/// - Enables A/B testing different position check strategies
/// </summary>
public interface IClientPositionProvider
{
    /// <summary>
    /// Checks if client has sufficient position to sell
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="symbol">Instrument symbol</param>
    /// <param name="quantity">Quantity to sell</param>
    /// <returns>True if client has sufficient position; false otherwise</returns>
    Task<bool> HasSufficientPositionAsync(
        string clientId,
        string symbol,
        decimal quantity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets client's current position in a symbol
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <param name="symbol">Instrument symbol</param>
    /// <returns>Current position details</returns>
    Task<ClientPosition?> GetPositionAsync(
        string clientId,
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all positions for a client
    /// </summary>
    /// <param name="clientId">Client ID</param>
    /// <returns>List of all client positions</returns>
    Task<List<ClientPosition>> GetAllPositionsAsync(
        string clientId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// PURPOSE: Encapsulate client position information
///
/// WHY CREATED:
/// - Rich return type instead of bool
/// - Carries position details for calculations
/// - Can track cost basis, P&L, etc.
/// - Useful for audit trail and reporting
///
/// USAGE:
/// var position = await positions.GetPositionAsync(clientId, symbol);
/// if (position?.Quantity >= orderQuantity) {
///     // Client has sufficient position
/// }
/// </summary>
public class ClientPosition
{
    public string ClientId { get; set; } = "";
    public string Symbol { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal AverageCostPerUnit { get; set; }
    public decimal CurrentValue { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// Calculate total cost basis
    /// </summary>
    public decimal TotalCostBasis => Quantity * AverageCostPerUnit;

    /// <summary>
    /// Calculate unrealized P&L
    /// </summary>
    public decimal UnrealizedPnL => CurrentValue - TotalCostBasis;
}
