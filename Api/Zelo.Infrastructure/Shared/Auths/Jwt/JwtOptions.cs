namespace Zelo.Infrastructure.Shared.Auths.Jwt;

public sealed class JwtOptions
{
    public string Issuer { get; }
    public string Audience { get; }
    public string SigningKey { get; }
    public TimeSpan Expiration { get; }

    public JwtOptions(string issuer, string audience, string signingKey, TimeSpan expiration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);
        ArgumentException.ThrowIfNullOrWhiteSpace(signingKey);

        if (System.Text.Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new ArgumentException(
                "JWT signing key must contain at least 32 UTF-8 bytes.",
                nameof(signingKey));
        }

        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(expiration));
        }

        Issuer = issuer;
        Audience = audience;
        SigningKey = signingKey;
        Expiration = expiration;
    }
}
