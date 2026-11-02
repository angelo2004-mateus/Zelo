namespace Zelo.Application.Shared.Auths;

public interface IUserAuthStore<TUser> where TUser : class
{
    Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default);
    Task<AuthUser<TUser>?> FindByIdentifierAsync(
        string identifier,
        CancellationToken cancellationToken = default);
    Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default);
    Task RecordFailedLoginAsync(Guid userId, CancellationToken cancellationToken = default);
    Task ClearFailedLoginsAsync(Guid userId, CancellationToken cancellationToken = default);
}
