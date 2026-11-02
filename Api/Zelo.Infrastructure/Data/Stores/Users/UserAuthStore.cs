using NHibernate;
using NHibernate.Linq;
using Zelo.Application.Shared.Auths;
using Zelo.Domain.Users;

namespace Zelo.Infrastructure.Data.Stores.Users;

public class UserAuthStore : IUserAuthStore<User>
{
    private readonly ISession _session;

    public UserAuthStore(ISession session)
    {
        _session = session;
    }

    public Task<bool> ExistsAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var email = identifier.Trim().ToLowerInvariant();
        return _session.Query<User>()
            .AnyAsync(user => user.Email.ToLower() == email, cancellationToken);
    }

    public Task<bool> IsActiveAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _session.Query<User>()
            .AnyAsync(user =>
                user.Id == userId &&
                user.Status == UserStatus.Active &&
                user.EmailConfirmationTime != null,
                cancellationToken);
    }

    public async Task RecordFailedLoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _session.CreateSQLQuery(
                "UPDATE userpasswordcredentials SET " +
                "lockoutendtime = CASE WHEN failedlogincount >= 4 THEN :lockout ELSE NULL END, " +
                "failedlogincount = CASE WHEN failedlogincount >= 4 THEN 0 ELSE failedlogincount + 1 END, " +
                "lastmodifiedtime = :now " +
                "WHERE userid = :userid AND (lockoutendtime IS NULL OR lockoutendtime <= :now)")
            .SetParameter("lockout", now.AddMinutes(15))
            .SetParameter("now", now)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task ClearFailedLoginsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _session.CreateSQLQuery(
                "UPDATE userpasswordcredentials SET failedlogincount = 0, " +
                "lockoutendtime = NULL, lastmodifiedtime = :now " +
                "WHERE userid = :userid AND (failedlogincount <> 0 OR lockoutendtime IS NOT NULL)")
            .SetParameter("now", DateTime.UtcNow)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task<AuthUser<User>?> FindByIdentifierAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var email = identifier.Trim().ToLowerInvariant();
        var user = await _session.Query<User>()
            .FirstOrDefaultAsync(
                candidate => candidate.Email.ToLower() == email,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var credential = await _session.Query<UserPasswordCredential>()
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == user.Id,
                cancellationToken);

        if (credential is null)
        {
            return null;
        }

        return new AuthUser<User>(
            user,
            user.Id,
            credential.PasswordHash,
            user.Status == UserStatus.Active,
            user.EmailConfirmationTime is not null,
            credential.LockoutEndTime > DateTime.UtcNow);
    }
}
