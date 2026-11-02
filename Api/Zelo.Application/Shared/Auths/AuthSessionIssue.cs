namespace Zelo.Application.Shared.Auths;

public class AuthSessionIssue
{
    public Guid UserId { get; }
    public Guid SessionId { get; }
    public string RefreshToken { get; }
    public DateTime ExpirationTime { get; }

    public AuthSessionIssue(Guid userId, Guid sessionId, string refreshToken, DateTime expirationTime)
    {
        UserId = userId;
        SessionId = sessionId;
        RefreshToken = refreshToken;
        ExpirationTime = expirationTime;
    }
}
