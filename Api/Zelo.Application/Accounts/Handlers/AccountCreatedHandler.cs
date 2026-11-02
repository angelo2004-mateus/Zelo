using Zelo.Application.Accounts.Services;
using Zelo.Application.Shared.Events;
using Zelo.Domain.Accounts.Events;

namespace Zelo.Application.Accounts.Handlers;

public class AccountCreatedHandler : IDomainEventHandler<AccountCreated>
{
    private readonly AccountSetupService _setup;

    public AccountCreatedHandler(AccountSetupService setup)
    {
        _setup = setup;
    }

    public async Task HandleAsync(AccountCreated domainEvent, CancellationToken cancellationToken)
    {
        await _setup.InitializeAsync(
            domainEvent.AccountId, domainEvent.CreatorUserId, cancellationToken);
    }
}
