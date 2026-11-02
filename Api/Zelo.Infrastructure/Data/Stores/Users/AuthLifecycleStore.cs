using NHibernate;
using NHibernate.Linq;
using Zelo.Application.Shared.Auths;
using Zelo.Domain.Users;

namespace Zelo.Infrastructure.Data.Stores.Users;

public class AuthLifecycleStore : IAuthLifecycleStore
{
    private readonly ISession _session;

    public AuthLifecycleStore(ISession session)
    {
        _session = session;
    }

    public async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await _session.Query<User>().FirstOrDefaultAsync(
            user => user.Email.ToLower() == email.ToLower(), cancellationToken);
    }

    public async Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _session.GetAsync<User>(userId, cancellationToken);
    }

    public async Task<UserPasswordCredential?> FindPasswordAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _session.Query<UserPasswordCredential>().FirstOrDefaultAsync(
            credential => credential.UserId == userId, cancellationToken);
    }

    public async Task<UserAuthToken?> FindTokenAsync(string hash, UserAuthTokenPurpose purpose, CancellationToken cancellationToken)
    {
        return await _session.Query<UserAuthToken>().FirstOrDefaultAsync(
            token => token.TokenHash == hash && token.Purpose == purpose,
            cancellationToken);
    }

    public Task AddTokenAsync(UserAuthToken token, CancellationToken cancellationToken)
    {
        return _session.SaveAsync(token, cancellationToken);
    }

    public async Task<bool> ConsumeTokenAsync(Guid tokenId, DateTime now, CancellationToken cancellationToken)
    {
        var count = await _session.CreateSQLQuery(
                "UPDATE userauthtokens SET consumptiontime = :now, lastmodifiedtime = :now " +
                "WHERE id = :id AND consumptiontime IS NULL AND expirationtime > :now")
            .SetParameter("now", now)
            .SetParameter("id", tokenId)
            .ExecuteUpdateAsync(cancellationToken);
        return count == 1;
    }

    public async Task ConsumeOutstandingTokensAsync(Guid userId, UserAuthTokenPurpose purpose, DateTime now, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE userauthtokens SET consumptiontime = :now, lastmodifiedtime = :now " +
                "WHERE userid = :userid AND purpose = :purpose AND consumptiontime IS NULL")
            .SetParameter("now", now)
            .SetParameter("userid", userId)
            .SetParameter("purpose", (int)purpose)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task RecordChallengeFailureAsync(Guid tokenId, DateTime now, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE userauthtokens SET failedattemptcount = failedattemptcount + 1, " +
                "consumptiontime = CASE WHEN failedattemptcount >= 4 THEN :now ELSE NULL END, " +
                "lastmodifiedtime = :now WHERE id = :id AND purpose = :purpose " +
                "AND consumptiontime IS NULL AND expirationtime > :now")
            .SetParameter("now", now)
            .SetParameter("id", tokenId)
            .SetParameter("purpose", (int)UserAuthTokenPurpose.TwoFactorChallenge)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task ConsumeRecoveryCodesAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE userauthtokens SET consumptiontime = :now, lastmodifiedtime = :now " +
                "WHERE userid = :userid AND purpose = :purpose AND consumptiontime IS NULL")
            .SetParameter("now", now)
            .SetParameter("userid", userId)
            .SetParameter("purpose", (int)UserAuthTokenPurpose.RecoveryCode)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public Task UpdateUserAsync(User user, CancellationToken cancellationToken)
    {
        return _session.UpdateAsync(user, cancellationToken);
    }

    public Task UpdatePasswordAsync(UserPasswordCredential credential, CancellationToken cancellationToken)
    {
        return _session.UpdateAsync(credential, cancellationToken);
    }

    public async Task<UserTotpCredential?> FindTotpAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _session.Query<UserTotpCredential>().FirstOrDefaultAsync(
            credential => credential.UserId == userId, cancellationToken);
    }

    public async Task SaveTotpAsync(UserTotpCredential credential, CancellationToken cancellationToken)
    {
        if (!await _session.Query<UserTotpCredential>().AnyAsync(
                existing => existing.Id == credential.Id, cancellationToken))
        {
            await _session.SaveAsync(credential, cancellationToken);
        }
        else
        {
            await _session.UpdateAsync(credential, cancellationToken);
        }
    }

    public async Task<bool> AcceptTotpStepAsync(Guid userId, long step, CancellationToken cancellationToken)
    {
        var count = await _session.CreateSQLQuery(
                "UPDATE usertotpcredentials SET lastusedstep = :step, failedattemptcount = 0, " +
                "lockoutendtime = NULL, lastmodifiedtime = :now " +
                "WHERE userid = :userid AND enabledtime IS NOT NULL " +
                "AND (lastusedstep IS NULL OR lastusedstep < :step) " +
                "AND (lockoutendtime IS NULL OR lockoutendtime <= :now)")
            .SetParameter("step", step)
            .SetParameter("now", DateTime.UtcNow)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
        return count == 1;
    }

    public async Task RecordTotpFailureAsync(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE usertotpcredentials SET " +
                "lockoutendtime = CASE WHEN failedattemptcount >= 4 THEN :lockout ELSE NULL END, " +
                "failedattemptcount = CASE WHEN failedattemptcount >= 4 THEN 0 " +
                "ELSE failedattemptcount + 1 END, lastmodifiedtime = :now " +
                "WHERE userid = :userid AND enabledtime IS NOT NULL " +
                "AND (lockoutendtime IS NULL OR lockoutendtime <= :now)")
            .SetParameter("lockout", now.AddMinutes(15))
            .SetParameter("now", now)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task ClearTotpFailuresAsync(Guid userId, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery(
                "UPDATE usertotpcredentials SET failedattemptcount = 0, " +
                "lockoutendtime = NULL, lastmodifiedtime = :now WHERE userid = :userid")
            .SetParameter("now", DateTime.UtcNow)
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }

    public async Task DeleteTotpAsync(Guid userId, CancellationToken cancellationToken)
    {
        await _session.CreateSQLQuery("DELETE FROM usertotpcredentials WHERE userid = :userid")
            .SetParameter("userid", userId)
            .ExecuteUpdateAsync(cancellationToken);
    }
}
