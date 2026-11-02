using Zelo.Application.Shared.Context;
using NHibernate;
using Zelo.Application.Shared.Data;
using Zelo.Domain.Users;
using Zelo.Infrastructure.Shared.Data.Repositories;

namespace Zelo.Infrastructure.Data.Repositories.Users;

public class UserRepository : Repository<User, Guid>, IUserRepository
{
    private readonly ISession _session;

    public UserRepository(ISession session, IUnitOfWork unitOfWork,
        ICurrentTenant currentTenant, ICurrentUser currentUser)
        : base(session, unitOfWork, currentTenant, currentUser)
    {
        _session = session;
    }

    public async Task AddWithCredentialAsync(User user, UserPasswordCredential credential, CancellationToken cancellationToken)
    {
        await CreateAsync(user, cancellationToken);
        await _session.SaveAsync(credential, cancellationToken);
    }
}
