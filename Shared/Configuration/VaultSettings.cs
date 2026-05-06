namespace Shared.Configuration;

public class VaultSettings
{
    public string Address { get; set; } = string.Empty;
    public string KeyName { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string SecretId { get; set; } = string.Empty;
}
