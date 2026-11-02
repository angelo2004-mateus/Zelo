using System.Security.Cryptography;
using System.Text;
using Zelo.Application.Shared.Auths;

namespace Zelo.Infrastructure.Shared.Auths;

public class AesSecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public AesSecretProtector(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
        {
            throw new ArgumentException("Auth:TotpEncryptionKey must be a Base64-encoded 32-byte key.");
        }
    }

    public string Protect(string secret)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(secret);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]);
    }

    public string Unprotect(string protectedSecret)
    {
        var bytes = Convert.FromBase64String(protectedSecret);
        var plaintext = new byte[bytes.Length - 28];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
