namespace Authentication_Service.Services;

/// <summary>
/// Create Private key - openssl genrsa -out private.pem 2048
/// Create Public key - openssl rsa -in private.pem -pubout -out public.pem
/// </summary>
public interface ITokenService
{
    string GenerateToken(string username);
}