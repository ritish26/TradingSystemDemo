using System.Security.Cryptography;

namespace Authentication_Service.Application.Interfaces;

public interface IRsaKeyProvider
{
    RSA GetPrivateKey();
}
