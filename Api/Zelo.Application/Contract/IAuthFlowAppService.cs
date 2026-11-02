using Zelo.Application.Model.Auths;

namespace Zelo.Application.Contract;

public interface IAuthFlowAppService
{
    Task SendConfirmationAsync(Guid userId, CancellationToken cancellationToken);
    Task ResendConfirmationAsync(string email, CancellationToken cancellationToken);
    Task<bool> ConfirmEmailAsync(string token, CancellationToken cancellationToken);
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task<bool> ResetPasswordAsync(
        string token, string password, CancellationToken cancellationToken);
    Task<string?> BeginTwoFactorChallengeAsync(Guid userId, CancellationToken cancellationToken);
    Task<Guid?> VerifyTwoFactorChallengeAsync(
        string token, string code, CancellationToken cancellationToken);
    Task<AuthTotpSetupDto?> SetupTotpAsync(
        Guid userId, string password, CancellationToken cancellationToken);
    Task<AuthTotpEnableDto?> EnableTotpAsync(
        Guid userId, string code, CancellationToken cancellationToken);
    Task<bool> DisableTotpAsync(
        Guid userId, string password, string code, CancellationToken cancellationToken);
}
