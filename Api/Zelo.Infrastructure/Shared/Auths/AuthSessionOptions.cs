namespace Zelo.Infrastructure.Shared.Auths;

public class AuthSessionOptions
{
    public TimeSpan RefreshExpiration { get; }

    public AuthSessionOptions(TimeSpan refreshExpiration)
    {
        if (refreshExpiration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(refreshExpiration));
        }

        RefreshExpiration = refreshExpiration;
    }
}
