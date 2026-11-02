namespace Zelo.Application.Shared.Auths;

public interface IAuthEmailSender
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken);
}
