namespace Zelo.Application.Model.Auths;

public class AuthTotpEnableDto
{
    public required IReadOnlyList<string> RecoveryCodes { get; set; }
}
