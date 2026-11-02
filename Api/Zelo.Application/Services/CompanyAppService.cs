using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using AutoMapper;
using Zelo.Application.Contract;
using Zelo.Application.Model.Companies;
using Zelo.Application.Shared.Authorization;
using Zelo.Application.Shared.Context;
using Zelo.Application.Shared.Crud;
using Zelo.Application.Shared.Data;
using Zelo.Application.Shared.Roles;
using Zelo.Domain.Companies;
using Zelo.Domain.Shared.Repositories;
using Zelo.Domain.UserTenants;

namespace Zelo.Application.Services;

public class CompanyAppService : CrudAppService<Company, Guid, CompanyDto, CompanyCreateRequestDto>, ICompanyAppService
{
    private readonly ITenantAuthorizationService _authorization;
    private readonly IUserTenantRepository _memberships;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;

    public CompanyAppService(IRepository<Company, Guid> companies, IUnitOfWork unitOfWork, IMapper mapper, ICurrentUser currentUser, ICurrentTenant currentTenant, ITenantAuthorizationService authorization, IUserTenantRepository memberships)
        : base(companies, unitOfWork, mapper, currentUser, currentTenant)
    {
        _authorization = authorization;
        _memberships = memberships;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
    }

    protected override async Task<bool> CanReadAsync(CancellationToken cancellationToken = default)
    {
        var access = await _authorization.GetAccessAsync(
            _currentUser.Id!.Value, _currentTenant.Id!.Value, cancellationToken);
        return access is not null;
    }

    protected override async Task<bool> CanWriteAsync(CancellationToken cancellationToken = default)
    {
        var access = await _authorization.GetAccessAsync(
            _currentUser.Id!.Value, _currentTenant.Id!.Value, cancellationToken);
        return access is not null && _authorization.HasPermission(access, PermissionCodes.CompanyManage);
    }

    protected override Task BeforeWriteAsync(CancellationToken cancellationToken)
    {
        return _memberships.LockTenantAsync(_currentTenant.Id!.Value, cancellationToken);
    }

    protected override Task OnBeforeCreateOrUpdateAsync(CrudWriteOperation operation, Company company, CancellationToken cancellationToken)
    {
        company.LegalName = company.LegalName?.Trim() ?? string.Empty;
        company.TradeName = company.TradeName?.Trim() ?? string.Empty;
        company.FederalTaxNumber = Regex.Replace(company.FederalTaxNumber ?? string.Empty, "[./\\s-]", string.Empty).ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(company.LegalName) || !Regex.IsMatch(company.FederalTaxNumber, "^[A-Z0-9]{12}[0-9]{2}$"))
        {
            throw new ValidationException("Company name or federal tax number is invalid.");
        }

        if (operation == CrudWriteOperation.Create)
        {
            company.Status = CompanyStatus.Active;
        }

        return Task.CompletedTask;
    }
}
