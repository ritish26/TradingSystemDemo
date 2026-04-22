namespace Shared.Infrastructure.Helper;

public class InMemoryIdempotencyStore : IIdempotencyStore
{
    private static readonly Dictionary<string, string> _store = new();

    public bool TryGet(string key, out string response)
    {
        return _store.TryGetValue(key, out response);
    }

    public void Save(string key, string response)
    {
        _store[key] = response;
    }
}