namespace Shared.Application.Interfaces;

// Abstraction for database initialization. Decouples services from specific DB implementation
// (PostgreSQL, MySQL, etc.). Follows DIP - high-level modules depend on abstraction, not concrete details.
public interface IDatabase
{
    Task InitializeAsync();
}
