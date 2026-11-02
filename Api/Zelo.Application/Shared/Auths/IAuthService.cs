namespace Zelo.Application.Shared.Auths;

public interface IAuthService<TUser, TAuthResult>
    where TUser : class
    where TAuthResult : AuthResult
{
    Task<TAuthResult> AuthenticateAsync(
        string identifier,
        string password,
        CancellationToken cancellationToken = default);
}
