namespace Zelo.Domain.Users;

public enum UserAuthTokenPurpose
{
    EmailConfirmation,
    PasswordReset,
    TwoFactorChallenge,
    RecoveryCode
}
