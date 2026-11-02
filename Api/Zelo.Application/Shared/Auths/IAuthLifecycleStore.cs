using Zelo.Domain.Users;

namespace Zelo.Application.Shared.Auths;

public interface IAuthLifecycleStore
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> FindUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserPasswordCredential?> FindPasswordAsync(Guid userId, CancellationToken cancellationToken);
    Task<UserAuthToken?> FindTokenAsync(
        string hash, UserAuthTokenPurpose purpose, CancellationToken cancellationToken);
    Task AddTokenAsync(UserAuthToken token, CancellationToken cancellationToken);
    Task<bool> ConsumeTokenAsync(Guid tokenId, DateTime now, CancellationToken cancellationToken);
    Task ConsumeOutstandingTokensAsync(
        Guid userId, UserAuthTokenPurpose purpose, DateTime now,
        CancellationToken cancellationToken);
    Task RecordChallengeFailureAsync(Guid tokenId, DateTime now, CancellationToken cancellationToken);
    Task ConsumeRecoveryCodesAsync(Guid userId, DateTime now, CancellationToken cancellationToken);
    Task UpdateUserAsync(User user, CancellationToken cancellationToken);
    Task UpdatePasswordAsync(UserPasswordCredential credential, CancellationToken cancellationToken);
    Task<UserTotpCredential?> FindTotpAsync(Guid userId, CancellationToken cancellationToken);
    Task SaveTotpAsync(UserTotpCredential credential, CancellationToken cancellationToken);
    Task<bool> AcceptTotpStepAsync(Guid userId, long step, CancellationToken cancellationToken);
    Task RecordTotpFailureAsync(Guid userId, DateTime now, CancellationToken cancellationToken);
    Task ClearTotpFailuresAsync(Guid userId, CancellationToken cancellationToken);
    Task DeleteTotpAsync(Guid userId, CancellationToken cancellationToken);
}
