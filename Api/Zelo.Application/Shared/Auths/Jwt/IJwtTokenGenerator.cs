namespace Zelo.Application.Shared.Auths.Jwt;

public interface IJwtTokenGenerator
{
    JwtToken GenerateToken(Guid userId, Guid sessionId);
}
