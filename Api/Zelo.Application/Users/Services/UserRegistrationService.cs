using System.ComponentModel.DataAnnotations;
using Zelo.Application.Shared.Auths;
using Zelo.Domain.Users;

namespace Zelo.Application.Users.Services;

public class UserRegistrationService
{
    private readonly IUserAuthStore<User> _userStore;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly IUserRepository _users;

    public UserRegistrationService(IUserAuthStore<User> userStore, IPasswordHasher<User> passwordHasher, IUserRepository users)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
        _users = users;
    }

    public async Task<User?> CreateAsync(string emailInput, string nameInput, string password, CancellationToken cancellationToken)
    {
        var email = emailInput?.Trim().ToLowerInvariant();
        var name = nameInput?.Trim();
        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(name) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 8)
        {
            throw new ArgumentException(
                "Valid email, password, and user name are required.");
        }

        if (await _userStore.ExistsAsync(email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            Name = name,
            UserRole = UserRole.User,
            Status = UserStatus.Active,
        };
        var credential = new UserPasswordCredential
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PasswordHash = _passwordHasher.HashPassword(user, password),
        };
        await _users.AddWithCredentialAsync(user, credential, cancellationToken);
        return user;
    }
}
