using ProcessingService.Application.Interfaces;

namespace ProcessingService.Infrastructure.Providers;

/// <summary>
/// PURPOSE: Provide client position data for execution validation
///
/// WHY CREATED:
/// - Implementation of IClientPositionProvider (Repository Pattern)
/// - Separates position logic from execution logic (SRP)
/// - Abstracts data source (database, service, cache)
/// - Testable: can mock or replace with test data
/// - Reusable: any service can check positions
///
/// SOLID PRINCIPLES:
/// - Single Responsibility: Only provides position data
/// - Dependency Inversion: Implements interface
/// - Open-Closed: Different data sources via new implementations
///
/// IMPLEMENTATION NOTES:
/// This is a simulated provider. In production:
/// - Query real portfolio database or service
/// - Cache positions for performance
/// - Add audit logging for compliance
/// - Implement versioning (positions as of what time?)
/// - Support T+0, T+1, T+2 settlement dates
///
/// REAL-WORLD EXAMPLE:
/// services.AddScoped<IClientPositionProvider, DatabasePositionProvider>();
/// // or
/// services.AddScoped<IClientPositionProvider, PortfolioServiceClient>();
/// // or (for testing)
/// services.AddScoped<IClientPositionProvider, MockPositionProvider>();
/// </summary>
public class ClientPositionProvider : IClientPositionProvider
{
    private readonly ILogger<ClientPositionProvider> _logger;

    public ClientPositionProvider(ILogger<ClientPositionProvider> logger)
    {
        _logger = logger;
    }

    public async Task<bool> HasSufficientPositionAsync(
        string clientId,
        string symbol,
        decimal quantity,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var position = await GetPositionAsync(clientId, symbol, cancellationToken);
            var hasSufficient = position != null && position.Quantity >= quantity;

            _logger.LogInformation(
                $"Client {clientId} position check for {symbol}: " +
                $"required={quantity}, available={position?.Quantity ?? 0}, sufficient={hasSufficient}");

            return hasSufficient;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking position for client {clientId} in {symbol}");
            return false;
        }
    }

    public async Task<ClientPosition?> GetPositionAsync(
        string clientId,
        string symbol,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate database query

        // Simulate position data
        // In production, this would query real portfolio database
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(symbol))
            return null;

        // Return simulated position (allows all sales for demo purposes)
        var position = new ClientPosition
        {
            ClientId = clientId,
            Symbol = symbol,
            Quantity = 1000m,  // Simulated: client has 1000 units
            AverageCostPerUnit = 100m,
            CurrentValue = 102000m, // Simulated: current market value
            LastUpdated = DateTime.UtcNow
        };

        _logger.LogDebug($"Position for client {clientId} in {symbol}: {position.Quantity} units");
        return position;
    }

    public async Task<List<ClientPosition>> GetAllPositionsAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        await Task.Delay(20, cancellationToken); // Simulate database query

        // Simulate portfolio
        return new List<ClientPosition>
        {
            new() { ClientId = clientId, Symbol = "AAPL", Quantity = 100m, AverageCostPerUnit = 150m, CurrentValue = 16000m },
            new() { ClientId = clientId, Symbol = "GOOG", Quantity = 50m, AverageCostPerUnit = 2800m, CurrentValue = 145000m },
            new() { ClientId = clientId, Symbol = "MSFT", Quantity = 200m, AverageCostPerUnit = 300m, CurrentValue = 65000m }
        };
    }
}
