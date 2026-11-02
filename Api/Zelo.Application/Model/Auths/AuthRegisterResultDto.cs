using Zelo.Application.Model.Accounts;
using Zelo.Application.Model.Users;

namespace Zelo.Application.Model.Auths;

public class AuthRegisterResultDto
{
    public bool Succeeded { get; set; }
    public AuthRegisterError Error { get; set; }
    public UserDto? User { get; set; }
    public AccountDto? Account { get; set; }
    public bool ConfirmationEmailSent { get; set; }
}
