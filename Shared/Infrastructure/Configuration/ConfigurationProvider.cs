using Microsoft.Extensions.Configuration;
using IApplicationConfigurationProvider = Shared.Application.Interfaces.IConfigurationProvider;

namespace Shared.Infrastructure.Configuration;

// Centralized configuration access. Validates and provides configuration values.
// Follows SRP - single responsibility for reading and validating configurations.
public class ConfigurationProvider : IApplicationConfigurationProvider
{
    private readonly IConfiguration _configuration;

    public ConfigurationProvider(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public T GetValue<T>(string key)
    {
        var value = _configuration[key];
        if (value == null)
            throw new InvalidOperationException($"Configuration key '{key}' not found");

        return ConvertValue<T>(value);
    }

    public T GetValue<T>(string key, T defaultValue)
    {
        var value = _configuration[key];
        if (value == null)
            return defaultValue;

        return ConvertValue<T>(value);
    }

    public string GetConnectionString(string name)
    {
        var connectionString = _configuration.GetConnectionString(name);
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException($"Connection string '{name}' not found");

        return connectionString;
    }

    private static T ConvertValue<T>(string value)
    {
        if (typeof(T) == typeof(int))
            return (T)(object)int.Parse(value);

        if (typeof(T) == typeof(string))
            return (T)(object)value;

        return (T)Convert.ChangeType(value, typeof(T));
    }
}
