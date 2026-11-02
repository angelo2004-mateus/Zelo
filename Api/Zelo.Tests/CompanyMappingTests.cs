using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Zelo.Application.Mapping;
using Zelo.Application.Model.Companies;
using Zelo.Domain.Companies;

namespace Zelo.Tests;

public class CompanyMappingTests
{
    [Fact]
    public void ProfilesAreValidAndEditingCannotReplaceProtectedFields()
    {
        var configuration = new MapperConfiguration(
            expression =>
            {
                expression.AddMaps(typeof(ApplicationMappingProfile).Assembly);
            },
            NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();

        var mapper = configuration.CreateMapper();
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var company = new Company
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            CreatorId = creatorId,
            CreationTime = DateTime.UtcNow.AddDays(-1),
            LegalName = "Old legal name",
            TradeName = "Old trade name",
            FederalTaxNumber = "12345678901234",
            Status = CompanyStatus.Active
        };
        var originalId = company.Id;
        var originalCreationTime = company.CreationTime;
        var request = new CompanyCreateRequestDto
        {
            LegalName = "New legal name",
            TradeName = "New trade name",
            FederalTaxNumber = "ABCDEFGHIJKL12"
        };

        mapper.Map(request, company);

        Assert.Equal(originalId, company.Id);
        Assert.Equal(tenantId, company.TenantId);
        Assert.Equal(creatorId, company.CreatorId);
        Assert.Equal(originalCreationTime, company.CreationTime);
        Assert.Equal(CompanyStatus.Active, company.Status);
        Assert.Equal("New legal name", company.LegalName);

        var response = mapper.Map<CompanyDto>(company);
        response.Id = Guid.NewGuid();
        response.TenantId = Guid.NewGuid();
        response.TradeName = "Updated by reverse map";
        mapper.Map(response, company);

        Assert.Equal(originalId, company.Id);
        Assert.Equal(tenantId, company.TenantId);
        Assert.Equal("Updated by reverse map", company.TradeName);
    }
}
