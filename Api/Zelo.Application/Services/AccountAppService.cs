using Zelo.Application.Contract;
using Zelo.Application.Model.Accounts;
using Zelo.Application.Shared.Context;
using Zelo.Domain.Accounts;

namespace Zelo.Application.Services;

public class AccountAppService : IAccountAppService
{
    private readonly IAccountRepository _accounts;
    private readonly ICurrentUser _currentUser;

    public AccountAppService(IAccountRepository accounts, ICurrentUser currentUser)
    {
        _accounts = accounts;
        _currentUser = currentUser;
    }

    public Task<IReadOnlyList<AccountAccessDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return ListAccessibleAsync(_currentUser.Id ?? Guid.Empty, cancellationToken);
    }

    public async Task<AccountAccessDto?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (_currentUser.Id is not Guid userId)
        {
            return null;
        }

        var account = await _accounts.GetAccessibleAsync(userId, tenantId, cancellationToken);
        return account is null ? null : ToDto(account);
    }

    public async Task<IReadOnlyList<AccountAccessDto>> ListAccessibleAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var access = await _accounts.ListAccessibleAsync(userId, cancellationToken);
        var results = new List<AccountAccessDto>(access.Count);

        foreach (var account in access)
        {
            results.Add(ToDto(account));
        }

        return results;
    }

    private static AccountAccessDto ToDto(AccountAccess account)
    {
        return new AccountAccessDto
        {
            AccountId = account.AccountId,
            AccountName = account.AccountName,
            RoleId = account.RoleId,
            RoleName = account.RoleName,
            IsPlatformAdminAccess = account.IsPlatformAdminAccess,
            IsActive = account.IsActive
        };
    }
}
