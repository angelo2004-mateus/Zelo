namespace Zelo.Application.Shared.Auths;

public class AuthService<TUser, TAuthResult> : IAuthService<TUser, TAuthResult>
    where TUser : class
    where TAuthResult : AuthResult, new()
{
    private readonly IUserAuthStore<TUser> _userStore;
    private readonly IPasswordHasher<TUser> _passwordHasher;

    public AuthService(IUserAuthStore<TUser> userStore, IPasswordHasher<TUser> passwordHasher)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
    }

    public virtual async Task<TAuthResult> AuthenticateAsync(string identifier, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrEmpty(password))
        {
            return InvalidCredentials();
        }

        var authUser = await _userStore.FindByIdentifierAsync(
            identifier.Trim(), cancellationToken);

        if (authUser is null)
        {
            return InvalidCredentials();
        }

        if (!authUser.IsActive || !authUser.IsConfirmed || authUser.IsLocked)
        {
            return InvalidCredentials();
        }

        if (!_passwordHasher.VerifyHashedPassword(authUser.User, authUser.PasswordHash, password))
        {
            await _userStore.RecordFailedLoginAsync(authUser.UserId, cancellationToken);
            return InvalidCredentials();
        }

        await _userStore.ClearFailedLoginsAsync(authUser.UserId, cancellationToken);

        return await CreateSuccessResultAsync(authUser, cancellationToken);
    }

    protected virtual Task<TAuthResult> CreateSuccessResultAsync(AuthUser<TUser> authUser, CancellationToken cancellationToken)
    {
        return Task.FromResult(new TAuthResult { Succeeded = true });
    }

    private static TAuthResult InvalidCredentials()
    {
        return new TAuthResult { Succeeded = false, Error = AuthError.InvalidCredentials };
    }
}
