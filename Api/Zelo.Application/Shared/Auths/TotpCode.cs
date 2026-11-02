using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Zelo.Application.Shared.Auths;

public static class TotpCode
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string CreateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        var output = new char[32];
        var bits = 0;
        var value = 0;
        var index = 0;
        foreach (var current in bytes)
        {
            value = (value << 8) | current;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                output[index++] = Alphabet[(value >> bits) & 31];
            }
        }

        return new string(output);
    }

    public static long? Verify(string secret, string code, DateTime time)
    {
        if (code.Length != 6 || !code.All(char.IsAsciiDigit))
        {
            return null;
        }

        var currentStep = new DateTimeOffset(time).ToUnixTimeSeconds() / 30;
        for (var step = currentStep - 1; step <= currentStep + 1; step++)
        {
            var candidate = Generate(secret, step);
            if (CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.ASCII.GetBytes(candidate),
                System.Text.Encoding.ASCII.GetBytes(code)))
            {
                return step;
            }
        }

        return null;
    }

    public static string Generate(string secret, long step)
    {
        var key = Decode(secret);
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);
        var digest = HMACSHA1.HashData(key, counter);
        var offset = digest[^1] & 15;
        var value = BinaryPrimitives.ReadUInt32BigEndian(digest.AsSpan(offset, 4)) & 0x7FFFFFFF;
        return (value % 1_000_000).ToString("D6");
    }

    private static byte[] Decode(string secret)
    {
        var output = new byte[secret.Length * 5 / 8];
        var bits = 0;
        var value = 0;
        var index = 0;
        foreach (var character in secret.ToUpperInvariant())
        {
            var digit = Alphabet.IndexOf(character);
            if (digit < 0)
            {
                throw new ArgumentException("Invalid TOTP secret.", nameof(secret));
            }

            value = (value << 5) | digit;
            bits += 5;
            if (bits >= 8)
            {
                bits -= 8;
                output[index++] = (byte)(value >> bits);
            }
        }

        return output;
    }
}
