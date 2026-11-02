using System.Security.Cryptography;

namespace Zelo.Application.Shared.Auths;

public static class AuthSecret
{
    public static string Create()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string value)
    {
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value)));
    }
}
