namespace Shared.Application.Interfaces;

public interface IIdempotencyStore
{
    bool TryGet(string key, out string response);
    void Save(string key, string response);
}