using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Zelo.Application.Mapping;
using Zelo.Application.Model.Companies;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Crud;
using Zelo.Application.Shared.Data;
using Zelo.Domain.Companies;
using Zelo.Domain.Shared.Entities;
using Zelo.Domain.Shared.Repositories;

namespace Zelo.Tests;

public class CrudAppServiceTests
{
    private static readonly IMapper Mapper = new MapperConfiguration(configuration =>
    {
        configuration.AddProfile<ApplicationMappingProfile>();
    }, NullLoggerFactory.Instance).CreateMapper();

    [Fact]
    public async Task TenantOwnedReadsRequireCurrentTenant()
    {
        var tenant = new FakeCurrentTenant();
        var repository = new FakeCompanyRepository(tenant);
        var service = new TestCompanyCrudAppService(repository, Mapper, new FakeCurrentUser(), tenant);

        var result = await service.GetAllAsync();

        Assert.Equal(AppServiceError.Invalid, result.Error);
        Assert.Equal(0, repository.ReadCount);
    }

    [Fact]
    public async Task CreateSetsTenantAndLeavesAuditToPersistence()
    {
        var user = new FakeCurrentUser { Id = Guid.NewGuid() };
        var tenant = new FakeCurrentTenant { Id = Guid.NewGuid() };
        var repository = new FakeCompanyRepository(tenant);
        var service = new TestCompanyCrudAppService(repository, Mapper, user, tenant);
        var request = new CompanyCreateRequestDto
        {
            LegalName = "Company",
            TradeName = "Trade",
            FederalTaxNumber = "12345678901234"
        };

        var result = await service.CreateAsync(request);

        Assert.True(result.Succeeded);
        Assert.Equal(tenant.Id, repository.Stored!.TenantId);
        Assert.Equal(Guid.Empty, repository.Stored.CreatorId);
        Assert.NotEqual(Guid.Empty, repository.Stored.Id);
        Assert.Equal(repository.Stored.Id, result.Value!.Id);
    }

    [Fact]
    public async Task UpdateKeepsProtectedFieldsAndDeleteRequiresOptIn()
    {
        var user = new FakeCurrentUser { Id = Guid.NewGuid() };
        var tenant = new FakeCurrentTenant { Id = Guid.NewGuid() };
        var repository = new FakeCompanyRepository(tenant)
        {
            Stored = new Company
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id.Value,
                CreatorId = user.Id.Value,
                CreationTime = DateTime.UtcNow.AddDays(-1),
                LegalName = "Before",
                TradeName = "Before",
                FederalTaxNumber = "12345678901234",
                Status = CompanyStatus.Active
            }
        };
        var originalId = repository.Stored.Id;
        var originalCreationTime = repository.Stored.CreationTime;
        var service = new TestCompanyCrudAppService(repository, Mapper, user, tenant);
        var request = new CompanyCreateRequestDto
        {
            LegalName = "After",
            TradeName = "After",
            FederalTaxNumber = "ABCDEFGHIJKL12"
        };

        var update = await service.UpdateAsync(originalId, request);
        var delete = await service.DeleteAsync(originalId);

        Assert.True(update.Succeeded);
        Assert.Equal("After", repository.Stored.LegalName);
        Assert.Equal(originalId, repository.Stored.Id);
        Assert.Equal(tenant.Id, repository.Stored.TenantId);
        Assert.Equal(user.Id, repository.Stored.CreatorId);
        Assert.Equal(originalCreationTime, repository.Stored.CreationTime);
        Assert.Equal(CompanyStatus.Active, repository.Stored.Status);
        Assert.Equal(AppServiceError.Conflict, delete.Error);
    }

    private sealed class TestCompanyCrudAppService : CrudAppService<Company, Guid, CompanyDto, CompanyCreateRequestDto>
    {
        public TestCompanyCrudAppService(IRepository<Company, Guid> repository, IMapper mapper,
            ICurrentUser user, ICurrentTenant tenant)
            : base(repository, new FakeUnitOfWork(), mapper, user, tenant)
        {
        }

        protected override Task<bool> CanReadAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        protected override Task<bool> CanWriteAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid? Id { get; set; }
    }

    private sealed class FakeCurrentTenant : ICurrentTenant
    {
        public Guid? Id { get; set; }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public void Track(IAggregateRoot aggregate)
        {
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action,
            CancellationToken cancellationToken = default)
        {
            return action(cancellationToken);
        }
    }

    private sealed class FakeCompanyRepository : IRepository<Company, Guid>
    {
        private readonly ICurrentTenant _tenant;

        public Company? Stored { get; set; }
        public int ReadCount { get; private set; }

        public FakeCompanyRepository(ICurrentTenant tenant)
        {
            _tenant = tenant;
        }

        public Task<Company?> GetAsync(Guid id)
        {
            ReadCount++;
            return Task.FromResult(Stored?.Id == id && Stored.TenantId == _tenant.Id ? Stored : null);
        }

        public Task<IReadOnlyList<Company>> GetAllAsync()
        {
            ReadCount++;
            IReadOnlyList<Company> results = Stored is not null && Stored.TenantId == _tenant.Id
                ? [Stored] : [];
            return Task.FromResult(results);
        }

        public Task<Company> CreateAsync(Company entity, CancellationToken cancellationToken = default)
        {
            Stored = entity;
            return Task.FromResult(entity);
        }

        public Task<Company> UpdateAsync(Company entity, CancellationToken cancellationToken = default)
        {
            Stored = entity;
            return Task.FromResult(entity);
        }

        public Task DeleteAsync(Company entity, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
