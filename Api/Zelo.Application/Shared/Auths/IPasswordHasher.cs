namespace Zelo.Application.Shared.Auths;

public interface IPasswordHasher<TUser> where TUser : class
{
    string HashPassword(TUser user, string password);
    bool VerifyHashedPassword(TUser user, string passwordHash, string password);
}
