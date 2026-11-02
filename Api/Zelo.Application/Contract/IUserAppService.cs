using Zelo.Application.Model.Auths;

namespace Zelo.Application.Contract;

public interface IUserAppService
{
    Task<AuthRegisterResultDto> RegisterAsync(
        AuthRegisterRequestDto request,
        CancellationToken cancellationToken = default);
    Task<AuthRegisterResultDto> RegisterUserAsync(
        AuthRegisterUserRequestDto request,
        CancellationToken cancellationToken = default);
}
