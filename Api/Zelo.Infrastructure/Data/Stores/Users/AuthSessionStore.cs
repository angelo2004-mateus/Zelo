using NHibernate;
using NHibernate.Linq;
using Zelo.Application.Shared.Auths;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Shared.Auths;

namespace Zelo.Infrastructure.Data.Stores.Users;

public class AuthSessionStore : IAuthSessionStore
{
    private readonly ISession _session;
    private readonly AuthSessionOptions _options;

    public AuthSessionStore(ISession session, AuthSessionOptions options)
    {
        _session = session;
        _options = options;
    }

    public async Task<AuthSessionIssue> CreateAsync(Guid userId, CancellationToken cancellationToken)
    {
        var refreshToken = AuthSecret.Create();
        var now = DateTime.UtcNow;
        var session = new UserAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RefreshTokenHash = AuthSecret.Hash(refreshToken),
            ExpirationTime = now.Add(_options.RefreshExpiration)
        };
        using var transaction = _session.BeginTransaction();
        await _session.SaveAsync(session, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AuthSessionIssue(
            userId, session.Id, refreshToken, session.ExpirationTime);
    }

    public async Task<AuthSessionIssue?> RotateAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var replacement = AuthSecret.Create();
        var oldHash = AuthSecret.Hash(refreshToken);
        var newHash = AuthSecret.Hash(replacement);
        var now = DateTime.UtcNow;
        var expirationTime = now.Add(_options.RefreshExpiration);
        using var transaction = _session.BeginTransaction();
        var changed = await _session.CreateSQLQuery(
                "UPDATE userauthsessions SET revocationtime = :now, lastmodifiedtime = :now " +
                "WHERE refreshtokenhash = :hash AND revocationtime IS NULL AND expirationtime > :now")
            .SetParameter("now", now)
            .SetParameter("hash", oldHash)
            .ExecuteUpdateAsync(cancellationToken);

        if (changed != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var oldSession = await _session.Query<UserAuthSession>()
            .FirstAsync(candidate => candidate.RefreshTokenHash == oldHash, cancellationToken);
        var newSession = new UserAuthSession
        {
            Id = Guid.NewGuid(),
            UserId = oldSession.UserId,
            RefreshTokenHash = newHash,
            ExpirationTime = expirationTime
        };
        await _session.SaveAsync(newSession, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new AuthSessionIssue(
            newSession.UserId, newSession.Id,
            replacement, newSession.ExpirationTime);
    }

    public Task<bool> IsActiveAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return _session.Query<UserAuthSession>().AnyAsync(
            candidate => candidate.Id == sessionId &&
                         candidate.UserId == userId &&
                         candidate.RevocationTime == null &&
                         candidate.ExpirationTime > now,
            cancellationToken);
    }

    public async Task RevokeAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE userauthsessions SET revocationtime = :now, lastmodifiedtime = :now " +
                "WHERE id = :id AND userid = :userid AND revocationtime IS NULL")
            .SetParameter("now", DateTime.UtcNow)
            .SetParameter("id", sessionId)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE userauthsessions SET revocationtime = :now, lastmodifiedtime = :now " +
                "WHERE userid = :userid AND revocationtime IS NULL")
            .SetParameter("now", DateTime.UtcNow)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }
}
