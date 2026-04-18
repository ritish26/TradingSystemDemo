namespace Authentication_Service.Services;

public interface ITokenService
{
    string GenerateToken(string username);
}