namespace Shared.Application.Interfaces;

// Provides type-safe configuration access. Centralizes config reading and validation (SRP).
// Supports both required values (throw if missing) and optional values (with defaults).
public interface IConfigurationProvider
{
    T GetValue<T>(string key);
    T GetValue<T>(string key, T defaultValue);
    string GetConnectionString(string name);
}
