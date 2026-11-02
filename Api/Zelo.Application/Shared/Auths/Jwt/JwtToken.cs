namespace Zelo.Application.Shared.Auths.Jwt;

public sealed class JwtToken
{
    public string Value { get; }
    public DateTime ExpirationTime { get; }

    public JwtToken(string value, DateTime expirationTime)
    {
        Value = value;
        ExpirationTime = expirationTime;
    }
}
