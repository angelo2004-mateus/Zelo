namespace Zelo.Application.Shared.Auths.Jwt;

public class JwtBearerAuthResult : AuthResult
{
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshExpirationTime { get; set; }
    public DateTime? ExpirationTime { get; set; }
}
