namespace Zelo.Application.Shared.Auths;

public interface IAuthSessionStore
{
    Task<AuthSessionIssue> CreateAsync(Guid userId, CancellationToken cancellationToken);
    Task<AuthSessionIssue?> RotateAsync(
        string refreshToken, CancellationToken cancellationToken);
    Task<bool> IsActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
    Task RevokeAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}
