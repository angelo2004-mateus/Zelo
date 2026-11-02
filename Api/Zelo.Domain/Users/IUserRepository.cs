using Zelo.Domain.Shared.Repositories;

namespace Zelo.Domain.Users;

public interface IUserRepository : IRepository<User, Guid>
{
    Task AddWithCredentialAsync(
        User user, UserPasswordCredential credential,
        CancellationToken cancellationToken);
}
