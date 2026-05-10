using AuthenticationService.Application.Models;

namespace AuthenticationService.Application.Interfaces;

public interface ITransitSigner
{
    Task<SigningResult> SignAsync(string input);
    Task<int> GetCurrentKeyVersionAsync();
}
