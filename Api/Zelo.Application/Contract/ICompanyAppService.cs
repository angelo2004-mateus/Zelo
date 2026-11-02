using Zelo.Application.Model.Companies;
using Zelo.Application.Shared.Crud;
using Zelo.Domain.Companies;

namespace Zelo.Application.Contract;

public interface ICompanyAppService : ICrudAppService<Company, Guid, CompanyDto, CompanyCreateRequestDto>
{
}
