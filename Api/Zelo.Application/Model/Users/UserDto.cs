namespace Zelo.Application.Model.Users;

public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string Name { get; set; }
}
