namespace Zelo.Application.Model.Auths;

public class AuthTotpSetupDto
{
    public required string Secret { get; set; }
    public required string AuthenticatorUri { get; set; }
}
