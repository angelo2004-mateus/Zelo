using Zelo.Application.Contract;
using Zelo.Application.Model.Auths;
using Zelo.Application.Model.Users;
using Zelo.Application.Shared.Auths;
using Zelo.Application.Shared.Auths.Jwt;
using Zelo.Domain.Users;

namespace Zelo.Application.Services;

public class ZeloJwtBearerAuthService : JwtBearerAuthService<User, AuthResultDto>, IAuthAppService
{
    private readonly IAccountAppService _accountAppService;
    private readonly IAuthFlowAppService _flow;
    private readonly IAuthSessionStore _sessions;
    private readonly IAuthLifecycleStore _store;

    public ZeloJwtBearerAuthService(IUserAuthStore<User> userStore, IPasswordHasher<User> passwordHasher, IJwtTokenGenerator tokenGenerator, IAccountAppService accountAppService, IAuthFlowAppService flow, IAuthSessionStore sessions, IAuthLifecycleStore store)
        : base(userStore, passwordHasher, tokenGenerator, sessions)
    {
        _accountAppService = accountAppService;
        _flow = flow;
        _sessions = sessions;
        _store = store;
    }

    protected override async Task<AuthResultDto> CreateSuccessResultAsync(AuthUser<User> authUser, CancellationToken cancellationToken)
    {
        var challenge = await _flow.BeginTwoFactorChallengeAsync(
            authUser.UserId, cancellationToken);
        if (challenge is not null)
        {
            return new AuthResultDto
            {
                Succeeded = true,
                RequiresTwoFactor = true,
                ChallengeToken = challenge
            };
        }

        var result = await base.CreateSuccessResultAsync(authUser, cancellationToken);
        return await AddProfileAsync(result, authUser.User, cancellationToken);
    }

    public async Task<AuthResultDto> VerifyTwoFactorAsync(string challengeToken, string code, CancellationToken cancellationToken)
    {
        var userId = await _flow.VerifyTwoFactorChallengeAsync(
            challengeToken, code, cancellationToken);
        if (userId is null)
        {
            return InvalidResult();
        }

        var user = await _store.FindUserByIdAsync(userId.Value, cancellationToken);
        if (user is null)
        {
            return InvalidResult();
        }

        var authUser = new AuthUser<User>(user, user.Id, string.Empty, true, true, false);
        var result = await base.CreateSuccessResultAsync(authUser, cancellationToken);
        return await AddProfileAsync(result, user, cancellationToken);
    }

    public async Task<AuthResultDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return InvalidResult();
        }

        var session = await _sessions.RotateAsync(
            refreshToken, cancellationToken);
        if (session is null)
        {
            return InvalidResult();
        }

        var user = await _store.FindUserByIdAsync(session.UserId, cancellationToken);
        if (user is null || user.Status != UserStatus.Active ||
            user.EmailConfirmationTime is null)
        {
            await _sessions.RevokeAsync(
                session.UserId, session.SessionId, cancellationToken);
            return InvalidResult();
        }

        var result = CreateResult(session);
        return await AddProfileAsync(result, user, cancellationToken);
    }

    public Task LogoutAsync(Guid userId, Guid sessionId, CancellationToken cancellationToken)
    {
        return _sessions.RevokeAsync(userId, sessionId, cancellationToken);
    }

    private async Task<AuthResultDto> AddProfileAsync(AuthResultDto result, User user, CancellationToken cancellationToken)
    {
        result.User = new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name
        };
        result.Accounts = await _accountAppService.ListAccessibleAsync(
            user.Id, cancellationToken);
        return result;
    }

    private static AuthResultDto InvalidResult()
    {
        return new AuthResultDto
        {
            Succeeded = false,
            Error = AuthError.InvalidCredentials
        };
    }
}
