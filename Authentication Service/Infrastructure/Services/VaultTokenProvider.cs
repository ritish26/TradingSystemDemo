using System.Text.Json;
using Authentication_Service.Application.Interfaces;
using Authentication_Service.Configuration;
using Microsoft.Extensions.Options;

namespace Authentication_Service.Infrastructure.Services;

public sealed class VaultTokenProvider : IVaultTokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly VaultSettings _vaultSettings;

    public VaultTokenProvider(HttpClient httpClient, IOptions<VaultSettings> vaultSettings)
    {
        _httpClient = httpClient;
        _vaultSettings = vaultSettings.Value;
    }

    public async Task<string> GetTokenAsync()
    {
        var url = $"{_vaultSettings.Address}/v1/auth/approle/login";

        var payload = new { role_id = _vaultSettings.RoleId, secret_id = _vaultSettings.SecretId };
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);
        var token = jsonDoc.RootElement.GetProperty("auth").GetProperty("client_token").GetString();

        return token ?? throw new InvalidOperationException("Failed to extract client_token from Vault response");
    }
}
