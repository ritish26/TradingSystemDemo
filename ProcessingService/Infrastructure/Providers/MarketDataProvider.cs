using ProcessingService.Application.Interfaces;
using ProcessingService.Domain.ValueObjects;

namespace ProcessingService.Infrastructure.Providers;

/// <summary>
/// PURPOSE: Provide market data and conditions
///
/// WHY CREATED:
/// - Implementation of IMarketDataProvider (Repository Pattern)
/// - Separates market data logic from execution logic (SRP)
/// - Testable: can mock or replace with different data source
/// - Reusable: any service can request market data
///
/// SOLID PRINCIPLES:
/// - Single Responsibility: Only provides market data
/// - Dependency Inversion: Implements interface, can be swapped
/// - Open-Closed: New data sources can be added without changing executor
///
/// IMPLEMENTATION NOTES:
/// This is a simulated provider. In production:
/// - Query real market data API (Bloomberg, Yahoo Finance, etc.)
/// - Implement caching for performance
/// - Add retry logic for resilience
/// - Support multiple fallback sources
/// - Add pricing tiers for different clients
///
/// FUTURE ENHANCEMENTS:
/// - Add caching layer: CachedMarketDataProvider
/// - Add fallback: FallbackMarketDataProvider (tries multiple sources)
/// - Add mock: MockMarketDataProvider (for testing)
/// - Add configuration: Load variance from settings
/// </summary>
public class MarketDataProvider : IMarketDataProvider
{
    private readonly ILogger<MarketDataProvider> _logger;

    public MarketDataProvider(ILogger<MarketDataProvider> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsMarketHealthyAsync(string symbol, CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await GetMarketStatusAsync(symbol, cancellationToken);
            return status.IsHealthy;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error checking market health for {symbol}");
            return false;
        }
    }

    public async Task<decimal> GetCurrentPriceAsync(string symbol, decimal requestedPrice, CancellationToken cancellationToken = default)
    {
        await Task.Delay(10, cancellationToken); // Simulate network latency

        // Simulate market price variance (-1% to +1%)
        var varianceDouble = new Random().NextDouble() * (double)ProcessingConstants.MaxPriceVariance - (double)(ProcessingConstants.MaxPriceVariance / 2);
        var variance = (decimal)varianceDouble;
        var currentPrice = requestedPrice * (1 + variance);

        _logger.LogInformation($"Market price for {symbol}: {currentPrice:C} (variance: {variance:P})");
        return currentPrice;
    }

    public async Task<MarketHealthStatus> GetMarketStatusAsync(string symbol, CancellationToken cancellationToken = default)
    {
        await Task.Delay(5, cancellationToken); // Simulate API call

        // Simulate market health check
        // In production, this would query actual market status
        if (string.IsNullOrWhiteSpace(symbol))
            return MarketHealthStatus.Unhealthy("Invalid symbol");

        return MarketHealthStatus.Healthy($"Market is open for {symbol}");
    }
}
