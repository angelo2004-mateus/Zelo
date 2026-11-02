using Zelo.Domain.Accounts;
using Zelo.Domain.Companies;
using Zelo.Domain.Invitations;
using Zelo.Domain.Roles;
using Zelo.Domain.Shared.Entities;
using Zelo.Domain.Shared.Events;
using Zelo.Domain.UserTenants;
using Zelo.Domain.Users;

namespace Zelo.Tests;

public class DomainEventsTests
{
    [Fact]
    public void EveryAggregateRootCanStoreAndPullEvents()
    {
        IAggregateRoot[] aggregates =
        [
            new User(),
            new Account(),
            new UserTenant(),
            new Company(),
            new Invitation()
        ];
        var domainEvent = new TestEvent();

        foreach (var aggregate in aggregates)
        {
            aggregate.AddEvent(domainEvent);
            Assert.Single(aggregate.DomainEvents);
            Assert.Same(domainEvent, Assert.Single(aggregate.PullEvents()));
            Assert.Empty(aggregate.DomainEvents);
            Assert.Empty(aggregate.PullEvents());
        }
    }

    [Fact]
    public void RoleIsAnEntityButNotAnAggregateRoot()
    {
        var role = new Role();

        Assert.IsAssignableFrom<IEntity>(role);
        Assert.IsNotAssignableFrom<IAggregateRoot>(role);
    }

    private sealed class TestEvent : IDomainEvent
    {
    }
}
