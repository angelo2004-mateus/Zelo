using Zelo.Domain.Accounts;
using Zelo.Domain.Accounts.Events;

namespace Zelo.Application.Accounts.Services;

public class AccountProvisioningService
{
    private readonly IAccountRepository _accounts;

    public AccountProvisioningService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<Account> CreateAsync(Guid creatorUserId, string name, CancellationToken cancellationToken)
    {
        var normalizedName = name?.Trim();
        if (creatorUserId == Guid.Empty ||
            string.IsNullOrWhiteSpace(normalizedName) ||
            normalizedName.Length > 200)
        {
            throw new ArgumentException("A creator and account name are required.");
        }

        var account = new Account
        {
            Id = Guid.NewGuid(),
            Name = normalizedName,
            Status = AccountStatus.Active,
            CreatorId = creatorUserId
        };
        account.AddEvent(new AccountCreated(account.Id, creatorUserId));
        await _accounts.CreateAsync(account, cancellationToken);
        return account;
    }
}
