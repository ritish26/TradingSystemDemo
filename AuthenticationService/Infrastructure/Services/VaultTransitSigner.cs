using System.Text.Json;
using AuthenticationService.Application.Interfaces;
using AuthenticationService.Application.Models;
using Microsoft.Extensions.Options;
using Shared.Application.Interfaces;
using Shared.Configuration;

namespace AuthenticationService.Infrastructure.Services;

public sealed class VaultTransitSigner : ITransitSigner
{
    private readonly HttpClient _httpClient;
    private readonly IVaultTokenProvider _tokenProvider;
    private readonly VaultSettings _vaultSettings;

    public VaultTransitSigner(
        HttpClient httpClient,
        IVaultTokenProvider tokenProvider,
        IOptions<VaultSettings> vaultSettings)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _vaultSettings = vaultSettings.Value;
    }

    public async Task<SigningResult> SignAsync(string input)
    {
        var token = await _tokenProvider.GetTokenAsync();
        var url = $"{_vaultSettings.Address}/v1/transit/sign/{_vaultSettings.KeyName}";

        var payload = new
        {
            input = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(input)),
            signature_algorithm = "pkcs1v15"
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };
        request.Headers.Add("X-Vault-Token", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);

        var signature = jsonDoc.RootElement.GetProperty("data").GetProperty("signature").GetString()
                        ?? throw new InvalidOperationException("Failed to extract signature from Vault response");

        var strippedSignature = StripVaultPrefix(signature);
        var base64UrlSignature = ConvertToBase64Url(strippedSignature);

        return new SigningResult(base64UrlSignature);
    }

    public async Task<int> GetCurrentKeyVersionAsync()
    {
        var token = await _tokenProvider.GetTokenAsync();
        var url = $"{_vaultSettings.Address}/v1/transit/keys/{_vaultSettings.KeyName}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("X-Vault-Token", token);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        using var jsonDoc = JsonDocument.Parse(responseBody);

        var latestVersion = jsonDoc.RootElement.GetProperty("data").GetProperty("latest_version").GetInt32();
        return latestVersion;
    }

    private static string StripVaultPrefix(string signature)
    {
        const string prefix = "vault:v";
        if (!signature.StartsWith(prefix))
            throw new InvalidOperationException($"Signature does not start with '{prefix}'");

        var colonIndex = signature.IndexOf(':', prefix.Length);
        if (colonIndex == -1)
            throw new InvalidOperationException("Invalid signature format");

        return signature[(colonIndex + 1)..];
    }

    private static string ConvertToBase64Url(string base64)
    {
        return base64.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
