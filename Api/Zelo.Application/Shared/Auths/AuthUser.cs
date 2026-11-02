namespace Zelo.Application.Shared.Auths;

public sealed class AuthUser<TUser> where TUser : class
{
    public TUser User { get; }
    public Guid UserId { get; }
    public string PasswordHash { get; }
    public bool IsActive { get; }
    public bool IsConfirmed { get; }
    public bool IsLocked { get; }

    public AuthUser(TUser user, Guid userId, string passwordHash, bool isActive, bool isConfirmed, bool isLocked)
    {
        User = user;
        UserId = userId;
        PasswordHash = passwordHash;
        IsActive = isActive;
        IsConfirmed = isConfirmed;
        IsLocked = isLocked;
    }
}
