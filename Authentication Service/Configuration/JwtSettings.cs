namespace Authentication_Service.Configuration;

public class JwtSettings
{
    public string PrivateKeyPath { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int TokenExpiryMinutes { get; set; } = 60;
}