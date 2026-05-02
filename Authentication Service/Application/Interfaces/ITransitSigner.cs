using Authentication_Service.Application.Models;

namespace Authentication_Service.Application.Interfaces;

public interface ITransitSigner
{
    Task<SigningResult> SignAsync(string input);
    Task<int> GetCurrentKeyVersionAsync();
}
