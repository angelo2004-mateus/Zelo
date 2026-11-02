using Zelo.Application.Contract;
using Zelo.Application.Model.Auths;
using Zelo.Application.Shared.Auths;
using Zelo.Application.Shared.Data;
using Zelo.Domain.Users;

namespace Zelo.Application.Services;

public class AuthFlowAppService : IAuthFlowAppService
{
    private readonly IAuthLifecycleStore _store;
    private readonly IAuthSessionStore _sessions;
    private readonly IAuthEmailSender _emailSender;
    private readonly ISecretProtector _protector;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUserAuthStore<User> _userAuthStore;
    private readonly IUnitOfWork _unitOfWork;

    public AuthFlowAppService(IAuthLifecycleStore store, IAuthSessionStore sessions, IAuthEmailSender emailSender, ISecretProtector protector, IPasswordHasher<User> passwordHasher, IUserAuthStore<User> userAuthStore, IUnitOfWork unitOfWork)
    {
        _store = store;
        _sessions = sessions;
        _emailSender = emailSender;
        _protector = protector;
        _passwordHasher = passwordHasher;
        _userAuthStore = userAuthStore;
        _unitOfWork = unitOfWork;
    }

    public async Task SendConfirmationAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByIdAsync(userId, cancellationToken);
        if (user is null || user.EmailConfirmationTime is not null)
        {
            return;
        }

        var token = await CreateTokenAsync(
            user.Id, UserAuthTokenPurpose.EmailConfirmation, TimeSpan.FromHours(24),
            cancellationToken);
        try
        {
            await _emailSender.SendAsync(
                user.Email, "Confirm your Zelo email",
                $"Confirmation token: {token}", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AuthEmailDeliveryException(exception);
        }
    }

    public async Task ResendConfirmationAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByEmailAsync(
            email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is not null)
        {
            await SendConfirmationAsync(user.Id, cancellationToken);
        }
    }

    public async Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken)
    {
        var record = await FindValidTokenAsync(
            token, UserAuthTokenPurpose.EmailConfirmation, cancellationToken);
        if (record is null)
        {
            return false;
        }

        var user = await _store.FindUserByIdAsync(record.UserId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        var succeeded = false;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            succeeded = await _store.ConsumeTokenAsync(record.Id, DateTime.UtcNow, ct);
            if (succeeded)
            {
                user.EmailConfirmationTime = DateTime.UtcNow;
                await _store.UpdateUserAsync(user, ct);
            }
        }, cancellationToken);
        return succeeded;
    }

    public async Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByEmailAsync(
            email.Trim().ToLowerInvariant(), cancellationToken);
        if (user is null || user.EmailConfirmationTime is null ||
            user.Status != UserStatus.Active)
        {
            return;
        }

        var token = await CreateTokenAsync(
            user.Id, UserAuthTokenPurpose.PasswordReset, TimeSpan.FromMinutes(30),
            cancellationToken);
        try
        {
            await _emailSender.SendAsync(
                user.Email, "Reset your Zelo password",
                $"Password reset token: {token}", cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new AuthEmailDeliveryException(exception);
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string password, CancellationToken cancellationToken)
    {
        if (password.Length < 8)
        {
            return false;
        }

        var record = await FindValidTokenAsync(
            token, UserAuthTokenPurpose.PasswordReset, cancellationToken);
        if (record is null)
        {
            return false;
        }

        var user = await _store.FindUserByIdAsync(record.UserId, cancellationToken);
        var credential = await _store.FindPasswordAsync(record.UserId, cancellationToken);
        if (user is null || credential is null)
        {
            return false;
        }

        var succeeded = false;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            succeeded = await _store.ConsumeTokenAsync(record.Id, DateTime.UtcNow, ct);
            if (succeeded)
            {
                credential.PasswordHash = _passwordHasher.HashPassword(user, password);
                credential.FailedLoginCount = 0;
                credential.LockoutEndTime = null;
                await _store.UpdatePasswordAsync(credential, ct);
                await _sessions.RevokeAllAsync(user.Id, ct);
            }
        }, cancellationToken);
        return succeeded;
    }

    public async Task<string?> BeginTwoFactorChallengeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var credential = await _store.FindTotpAsync(userId, cancellationToken);
        if (credential?.EnabledTime is null)
        {
            return null;
        }

        return await CreateTokenAsync(
            userId, UserAuthTokenPurpose.TwoFactorChallenge,
            TimeSpan.FromMinutes(5), cancellationToken);
    }

    public async Task<Guid?> VerifyTwoFactorChallengeAsync(string token, string code, CancellationToken cancellationToken)
    {
        var challenge = await FindValidTokenAsync(
            token, UserAuthTokenPurpose.TwoFactorChallenge, cancellationToken);
        if (challenge is null)
        {
            return null;
        }

        var credential = await _store.FindTotpAsync(challenge.UserId, cancellationToken);
        var user = await _store.FindUserByIdAsync(challenge.UserId, cancellationToken);
        if (credential?.EnabledTime is null ||
            credential.LockoutEndTime > DateTime.UtcNow || user is null ||
            user.Status != UserStatus.Active || user.EmailConfirmationTime is null)
        {
            return null;
        }

        var step = TotpCode.Verify(
            _protector.Unprotect(credential.ProtectedSecret), code, DateTime.UtcNow);
        UserAuthToken? recovery = null;
        if (step is null)
        {
            recovery = await FindValidTokenAsync(
                code, UserAuthTokenPurpose.RecoveryCode, cancellationToken);
        }
        if (step is null && (recovery is null || recovery.UserId != user.Id))
        {
            await _store.RecordTotpFailureAsync(user.Id, DateTime.UtcNow, cancellationToken);
            await _store.RecordChallengeFailureAsync(
                challenge.Id, DateTime.UtcNow, cancellationToken);
            return null;
        }

        var succeeded = false;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            bool accepted;
            if (step is not null)
            {
                accepted = await _store.AcceptTotpStepAsync(user.Id, step.Value, ct);
            }
            else
            {
                accepted = await _store.ConsumeTokenAsync(
                    recovery!.Id, DateTime.UtcNow, ct);
            }
            if (accepted)
            {
                succeeded = await _store.ConsumeTokenAsync(challenge.Id, DateTime.UtcNow, ct);
                if (succeeded && recovery is not null)
                {
                    await _store.ClearTotpFailuresAsync(user.Id, ct);
                }
            }
        }, cancellationToken);
        if (!succeeded)
        {
            await _store.RecordTotpFailureAsync(user.Id, DateTime.UtcNow, cancellationToken);
            await _store.RecordChallengeFailureAsync(
                challenge.Id, DateTime.UtcNow, cancellationToken);
        }

        if (succeeded)
        {
            return user.Id;
        }

        return null;
    }

    public async Task<AuthTotpSetupDto?> SetupTotpAsync(Guid userId, string password, CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByIdAsync(userId, cancellationToken);
        var passwordCredential = await _store.FindPasswordAsync(userId, cancellationToken);
        if (user is null || passwordCredential is null ||
            passwordCredential.LockoutEndTime > DateTime.UtcNow)
        {
            return null;
        }

        if (!_passwordHasher.VerifyHashedPassword(
            user, passwordCredential.PasswordHash, password))
        {
            await _userAuthStore.RecordFailedLoginAsync(userId, cancellationToken);
            return null;
        }

        await _userAuthStore.ClearFailedLoginsAsync(userId, cancellationToken);

        var credential = await _store.FindTotpAsync(userId, cancellationToken);
        if (credential?.EnabledTime is not null)
        {
            return null;
        }

        var secret = TotpCode.CreateSecret();
        credential ??= new UserTotpCredential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
        };
        credential.ProtectedSecret = _protector.Protect(secret);
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await _store.SaveTotpAsync(credential, ct);
        }, cancellationToken);

        return new AuthTotpSetupDto
        {
            Secret = secret,
            AuthenticatorUri =
                $"otpauth://totp/Zelo:{Uri.EscapeDataString(user.Email)}" +
                $"?secret={secret}&issuer=Zelo&digits=6&period=30"
        };
    }

    public async Task<AuthTotpEnableDto?> EnableTotpAsync(Guid userId, string code, CancellationToken cancellationToken)
    {
        var credential = await _store.FindTotpAsync(userId, cancellationToken);
        if (credential is null || credential.EnabledTime is not null)
        {
            return null;
        }

        var step = TotpCode.Verify(
            _protector.Unprotect(credential.ProtectedSecret), code, DateTime.UtcNow);
        if (step is null)
        {
            return null;
        }

        var recoveryCodes = new string[8];
        for (var index = 0; index < recoveryCodes.Length; index++)
        {
            recoveryCodes[index] = AuthSecret.Create();
        }
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            credential.EnabledTime = DateTime.UtcNow;
            credential.LastUsedStep = step.Value;
            await _store.SaveTotpAsync(credential, ct);
            foreach (var recoveryCode in recoveryCodes)
            {
                await _store.AddTokenAsync(new UserAuthToken
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    TokenHash = AuthSecret.Hash(recoveryCode),
                    Purpose = UserAuthTokenPurpose.RecoveryCode,
                    ExpirationTime = DateTime.UtcNow.AddYears(10)
                }, ct);
            }

            await _sessions.RevokeAllAsync(userId, ct);
        }, cancellationToken);
        return new AuthTotpEnableDto { RecoveryCodes = recoveryCodes };
    }

    public async Task<bool> DisableTotpAsync(Guid userId, string password, string code, CancellationToken cancellationToken)
    {
        var user = await _store.FindUserByIdAsync(userId, cancellationToken);
        var passwordCredential = await _store.FindPasswordAsync(userId, cancellationToken);
        var credential = await _store.FindTotpAsync(userId, cancellationToken);
        if (user is null || passwordCredential is null ||
            credential?.EnabledTime is null ||
            credential.LockoutEndTime > DateTime.UtcNow ||
            passwordCredential.LockoutEndTime > DateTime.UtcNow)
        {
            return false;
        }

        if (!_passwordHasher.VerifyHashedPassword(
            user, passwordCredential.PasswordHash, password))
        {
            await _userAuthStore.RecordFailedLoginAsync(userId, cancellationToken);
            return false;
        }

        await _userAuthStore.ClearFailedLoginsAsync(userId, cancellationToken);

        var step = TotpCode.Verify(
            _protector.Unprotect(credential.ProtectedSecret), code, DateTime.UtcNow);
        UserAuthToken? recovery = null;
        if (step is null)
        {
            recovery = await FindValidTokenAsync(
                code, UserAuthTokenPurpose.RecoveryCode, cancellationToken);
        }
        if (step is null && (recovery is null || recovery.UserId != userId))
        {
            await _store.RecordTotpFailureAsync(userId, DateTime.UtcNow, cancellationToken);
            return false;
        }

        var succeeded = false;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (step is not null)
            {
                succeeded = await _store.AcceptTotpStepAsync(
                    userId, step.Value, ct);
            }
            else
            {
                succeeded = await _store.ConsumeTokenAsync(
                    recovery!.Id, DateTime.UtcNow, ct);
            }
            if (succeeded)
            {
                await _store.ConsumeRecoveryCodesAsync(userId, DateTime.UtcNow, ct);
                await _store.DeleteTotpAsync(userId, ct);
                await _sessions.RevokeAllAsync(userId, ct);
            }
        }, cancellationToken);
        if (!succeeded)
        {
            await _store.RecordTotpFailureAsync(userId, DateTime.UtcNow, cancellationToken);
        }

        return succeeded;
    }

    private async Task<string> CreateTokenAsync(Guid userId, UserAuthTokenPurpose purpose, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var raw = AuthSecret.Create();
        var now = DateTime.UtcNow;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            if (purpose is UserAuthTokenPurpose.EmailConfirmation or
                UserAuthTokenPurpose.PasswordReset)
            {
                await _store.ConsumeOutstandingTokensAsync(userId, purpose, now, ct);
            }

            await _store.AddTokenAsync(new UserAuthToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = AuthSecret.Hash(raw),
                Purpose = purpose,
                ExpirationTime = now.Add(lifetime)
            }, ct);
        }, cancellationToken);
        return raw;
    }

    private async Task<UserAuthToken?> FindValidTokenAsync(string raw, UserAuthTokenPurpose purpose, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var token = await _store.FindTokenAsync(
            AuthSecret.Hash(raw), purpose, cancellationToken);
        if (token is { ConsumptionTime: null } &&
            token.ExpirationTime > DateTime.UtcNow)
        {
            return token;
        }

        return null;
    }
}
