using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zelo.Application.Contract;
using Zelo.Application.Model.Companies;
using Zelo.Domain.Companies;
using Zelo.Web.Api.Shared.Controllers;

namespace Zelo.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts/{tenantId:guid}/companies")]
public class CompaniesController : CrudApiController<Company, Guid, CompanyDto, CompanyCreateRequestDto, ICompanyAppService>
{
    public CompaniesController(ICompanyAppService companies) : base(companies)
    {
    }

    [HttpGet]
    public Task<IActionResult> GetAll()
    {
        return GetAllAsync();
    }

    [HttpPost]
    public Task<IActionResult> Create(CompanyCreateRequestDto request)
    {
        return CreateAsync(request);
    }
}
