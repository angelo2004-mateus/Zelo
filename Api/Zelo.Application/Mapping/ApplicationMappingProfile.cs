using AutoMapper;
using Zelo.Application.Model.Accounts;
using Zelo.Application.Model.Companies;
using Zelo.Application.Model.Users;
using Zelo.Domain.Accounts;
using Zelo.Domain.Companies;
using Zelo.Domain.Shared.Entities;
using Zelo.Domain.Users;

namespace Zelo.Application.Mapping;

public class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateEntityMap<User, UserDto>();
        CreateEntityMap<Account, AccountDto>();
        CreateEntityMap<Company, CompanyDto>();
        CreateEntityMap<Company, CompanyCreateRequestDto>();
    }

    private void CreateEntityMap<TEntity, TDto>() where TEntity : Entity
    {
        var reverse = CreateMap<TEntity, TDto>().ReverseMap();

        if (typeof(TDto).GetProperty(nameof(Entity.Id)) is not null)
        {
            reverse.ForMember(entity => entity.Id, options => { options.Ignore(); });
        }

        if (typeof(TEntity).GetProperty(nameof(IMultiTenant.TenantId)) is not null &&
            typeof(TDto).GetProperty(nameof(IMultiTenant.TenantId)) is not null)
        {
            reverse.ForMember(nameof(IMultiTenant.TenantId), options => { options.Ignore(); });
        }
    }
}
