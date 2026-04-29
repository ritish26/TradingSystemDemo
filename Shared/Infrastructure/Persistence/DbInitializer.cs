using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Application.Interfaces;

namespace Shared.Infrastructure.Persistence;

// Implements IDatabase for PostgreSQL. Handles schema initialization via SQL scripts.
// Uses abstraction (IConfigurationProvider) to decouple from configuration source.
public class PostgreSqlDatabase : IDatabase
{
    private readonly string _connectionString;
    private readonly ILogger<PostgreSqlDatabase> _logger;

    public PostgreSqlDatabase(IConfigurationProvider configProvider, ILogger<PostgreSqlDatabase> logger)
    {
        _connectionString = configProvider.GetConnectionString("DefaultConnection");
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        try
        {
            _logger.LogInformation("Starting database initialization...");

            await using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            var ordersTableScript = LoadScript("CreateOrdersTable.sql");
            await ExecuteScriptAsync(connection, ordersTableScript);

            var outboxTableScript = LoadScript("CreateOutBoxTable.sql");
            await ExecuteScriptAsync(connection, outboxTableScript);

            _logger.LogInformation("Database initialized.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DB init failed");
            throw;
        }
    }

    private static string LoadScript(string scriptName)
    {
        var scriptPath = Path.Combine(
            AppContext.BaseDirectory,
            "Infrastructure",
            "Persistence",
            scriptName
        );

        return File.ReadAllText(scriptPath);
    }

    private static async Task ExecuteScriptAsync(NpgsqlConnection connection, string script)
    {
        await using var command = new NpgsqlCommand(script, connection);
        await command.ExecuteNonQueryAsync();
    }
}