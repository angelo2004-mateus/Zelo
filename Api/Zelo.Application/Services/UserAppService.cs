using AutoMapper;
using Zelo.Application.Accounts.Services;
using Zelo.Application.Contract;
using Zelo.Application.Model.Accounts;
using Zelo.Application.Model.Auths;
using Zelo.Application.Model.Users;
using Zelo.Application.Shared.Auths;
using Zelo.Application.Shared.Data;
using Zelo.Application.Users.Services;
using Zelo.Domain.Accounts;
using Zelo.Domain.Users;

namespace Zelo.Application.Services;

public class UserAppService : IUserAppService
{
    private readonly UserRegistrationService _registration;
    private readonly AccountProvisioningService _accounts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuthFlowAppService _authFlow;
    private readonly IMapper _mapper;

    public UserAppService(UserRegistrationService registration, AccountProvisioningService accounts, IUnitOfWork unitOfWork, IAuthFlowAppService authFlow, IMapper mapper)
    {
        _registration = registration;
        _accounts = accounts;
        _unitOfWork = unitOfWork;
        _authFlow = authFlow;
        _mapper = mapper;
    }

    public async Task<AuthRegisterResultDto> RegisterAsync(AuthRegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var accountName = request.AccountName?.Trim();
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new ArgumentException("Account name is required.", nameof(request));
        }

        User? user = null;
        Account? account = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            user = await _registration.CreateAsync(
                request.Email, request.Name, request.Password, ct);
            if (user is null)
            {
                return;
            }

            account = await _accounts.CreateAsync(user.Id, accountName, ct);
        }, cancellationToken);

        if (user is null)
        {
            return new AuthRegisterResultDto
            {
                Error = AuthRegisterError.EmailAlreadyInUse
            };
        }

        var confirmationEmailSent = await TrySendConfirmationAsync(
            user.Id, cancellationToken);
        return new AuthRegisterResultDto
        {
            Succeeded = true,
            ConfirmationEmailSent = confirmationEmailSent,
            User = _mapper.Map<UserDto>(user),
            Account = _mapper.Map<AccountDto>(account!)
        };
    }

    public async Task<AuthRegisterResultDto> RegisterUserAsync(AuthRegisterUserRequestDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        User? user = null;
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            user = await _registration.CreateAsync(
                request.Email, request.Name, request.Password, ct);
        }, cancellationToken);

        if (user is null)
        {
            return new AuthRegisterResultDto
            {
                Error = AuthRegisterError.EmailAlreadyInUse
            };
        }

        var confirmationEmailSent = await TrySendConfirmationAsync(
            user.Id, cancellationToken);
        return new AuthRegisterResultDto
        {
            Succeeded = true,
            ConfirmationEmailSent = confirmationEmailSent,
            User = _mapper.Map<UserDto>(user)
        };
    }

    private async Task<bool> TrySendConfirmationAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await _authFlow.SendConfirmationAsync(userId, cancellationToken);
            return true;
        }
        catch (AuthEmailDeliveryException)
        {
            return false;
        }
    }

}
