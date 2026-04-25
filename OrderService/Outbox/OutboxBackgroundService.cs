namespace OrderService.Outbox;

public class OutboxBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxBackgroundService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var processor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

            await processor.ProcessAsync();

            await Task.Delay(5000, stoppingToken); // every 5 sec
        }
    }
}