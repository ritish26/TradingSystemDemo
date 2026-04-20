namespace Authentication_Service.Configuration;

public class JwtSettings
{
    public string PrivateKeyPath { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
}