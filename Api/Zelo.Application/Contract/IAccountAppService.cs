using Zelo.Application.Model.Accounts;

namespace Zelo.Application.Contract;

public interface IAccountAppService
{
    Task<IReadOnlyList<AccountAccessDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AccountAccessDto?> GetAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AccountAccessDto>> ListAccessibleAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
