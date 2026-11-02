namespace Zelo.Application.Shared.Auths.Jwt;

public class JwtBearerAuthService<TUser, TAuthResult> : AuthService<TUser, TAuthResult>
    where TUser : class
    where TAuthResult : JwtBearerAuthResult, new()
{
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IAuthSessionStore _sessionStore;

    public JwtBearerAuthService(IUserAuthStore<TUser> userStore, IPasswordHasher<TUser> passwordHasher, IJwtTokenGenerator tokenGenerator, IAuthSessionStore sessionStore)
        : base(userStore, passwordHasher)
    {
        _tokenGenerator = tokenGenerator;
        _sessionStore = sessionStore;
    }

    protected override async Task<TAuthResult> CreateSuccessResultAsync(AuthUser<TUser> authUser, CancellationToken cancellationToken)
    {
        var session = await _sessionStore.CreateAsync(
            authUser.UserId, cancellationToken);
        return CreateResult(session);
    }

    protected TAuthResult CreateResult(AuthSessionIssue session)
    {
        var token = _tokenGenerator.GenerateToken(
            session.UserId, session.SessionId);
        return new TAuthResult
        {
            Succeeded = true,
            AccessToken = token.Value,
            RefreshToken = session.RefreshToken,
            ExpirationTime = token.ExpirationTime,
            RefreshExpirationTime = session.ExpirationTime
        };
    }
}
