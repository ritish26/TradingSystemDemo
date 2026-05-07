using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OrderService.Application.Interfaces;
using Shared.Application.Interfaces;
using Shared.Configuration;

namespace OrderService.Infrastructure.Services;

public sealed class VaultPublicKeyProvider : IPublicKeyProvider
{
    private readonly HttpClient _httpClient;
    private readonly IVaultTokenProvider _vaultTokenProvider;
    private readonly VaultSettings _vaultSettings;

    public VaultPublicKeyProvider(
        HttpClient httpClient,
        IVaultTokenProvider vaultTokenProvider,
        IOptions<VaultSettings> vaultSettings)
    {
        _httpClient = httpClient;
        _vaultTokenProvider = vaultTokenProvider;
        _vaultSettings = vaultSettings.Value;
    }

    public async Task<RsaSecurityKey> GetPublicKeyAsync(string kid)
    {
        var token = await _vaultTokenProvider.GetTokenAsync();
        var url = $"{_vaultSettings.Address}/v1/transit/keys/{_vaultSettings.KeyName}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Vault-Token", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);

        var keys = jsonDoc.RootElement.GetProperty("data").GetProperty("keys");

        if (!keys.TryGetProperty(kid, out var keyEntry))
            throw new InvalidOperationException($"Key version '{kid}' not found in Vault");

        var publicKeyPem = keyEntry.GetProperty("public_key").GetString()
            ?? throw new InvalidOperationException($"public_key is null for kid '{kid}'");

        var pemContent = publicKeyPem
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim();

        var derBytes = Convert.FromBase64String(pemContent);

        var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(derBytes, out _);
        var rsaSecurityKey = new RsaSecurityKey(rsa)
        {
            KeyId = kid    
        };

        return rsaSecurityKey;
    }
}
