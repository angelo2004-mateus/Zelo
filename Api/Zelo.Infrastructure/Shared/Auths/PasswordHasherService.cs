using Microsoft.AspNetCore.Identity;
using Zelo.Application.Shared.Auths;

namespace Zelo.Infrastructure.Shared.Auths;

public class PasswordHasherService<TUser> :
    Zelo.Application.Shared.Auths.IPasswordHasher<TUser> where TUser : class
{
    private readonly PasswordHasher<TUser> _hasher = new();

    public string HashPassword(TUser user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyHashedPassword(TUser user, string passwordHash, string password)
    {
        return _hasher.VerifyHashedPassword(user, passwordHash, password)
            != PasswordVerificationResult.Failed;
    }
}
