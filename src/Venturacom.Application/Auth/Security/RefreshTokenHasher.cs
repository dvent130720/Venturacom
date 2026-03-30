using System.Security.Cryptography;
using System.Text;

namespace Venturacom.Application.Auth.Security;

public static class RefreshTokenHasher
{
    public static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
