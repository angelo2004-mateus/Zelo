using Zelo.Application.Model.Auths;
using Zelo.Application.Shared.Auths;
using Zelo.Domain.Users;

namespace Zelo.Application.Contract;

public interface IAuthAppService : IAuthService<User, AuthResultDto>
{
    Task<AuthResultDto> VerifyTwoFactorAsync(
        string challengeToken, string code, CancellationToken cancellationToken);
    Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken);
    Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken);
}
