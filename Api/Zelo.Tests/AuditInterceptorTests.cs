using Zelo.Application.Shared.Context;
using Zelo.Domain.Companies;
using Zelo.Infrastructure.Shared.Data;

namespace Zelo.Tests;

public class AuditInterceptorTests
{
    [Fact]
    public void SaveAndUpdateStampEntityAndNhState()
    {
        var userId = Guid.NewGuid();
        var interceptor = new AuditInterceptor(new TestCurrentUser { Id = userId });
        var company = new Company();
        var properties = new[] { "CreationTime", "CreatorId", "LastModifiedTime", "LastModifierId" };
        var state = new object[properties.Length];

        Assert.True(interceptor.OnSave(company, Guid.NewGuid(), state, properties, []));
        Assert.Equal(company.CreationTime, state[0]);
        Assert.Equal(userId, company.CreatorId);
        Assert.Equal(userId, state[1]);

        Assert.True(interceptor.OnFlushDirty(company, company.Id, state, [], properties, []));
        Assert.Equal(company.LastModifiedTime, state[2]);
        Assert.Equal(userId, company.LastModifierId);
        Assert.Equal(userId, state[3]);
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public Guid? Id { get; set; }
    }
}
