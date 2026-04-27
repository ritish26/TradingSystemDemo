using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Shared.Infrastructure.Persistence;

public class DbInitializer
{
    private readonly string _connectionString;
    private readonly ILogger<DbInitializer> _logger;

    public DbInitializer(IConfiguration configuration, ILogger<DbInitializer> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("DefaultConnection not found");

        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();
        
            var scriptPath = Path.Combine(
                AppContext.BaseDirectory,
                "Infrastructure",
                "Persistence",
                "CreateOrdersTable.sql"
            );

            var outboxPath = Path.Combine(
                AppContext.BaseDirectory,
                "Infrastructure",
                "Persistence",
                "CreateOutBoxTable.sql"
            );
        
            var script = await File.ReadAllTextAsync(scriptPath); 
            await using var command = new NpgsqlCommand(script, connection);
            await command.ExecuteNonQueryAsync();  
        
            var script1 = await File.ReadAllTextAsync(outboxPath); 
            await using var command1 = new NpgsqlCommand(script1, connection);
            await command1.ExecuteNonQueryAsync();  

            _logger.LogInformation("Database initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB init failed");
            throw;
        }
    }
}