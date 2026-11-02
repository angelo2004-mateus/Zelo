using System.Text;
using Zelo.Application.Shared.Auths;

namespace Zelo.Infrastructure.Shared.Auths;

public class FileAuthEmailSender : IAuthEmailSender
{
    private readonly string _directory;

    public FileAuthEmailSender(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(_directory);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                _directory,
                UnixFileMode.UserRead |
                UnixFileMode.UserWrite |
                UnixFileMode.UserExecute);
        }
    }

    public Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var content = $"To: {recipient}\nSubject: {subject}\n\n{body}\n";
        var path = Path.Combine(_directory, $"{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}.txt");
        return File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);
    }
}
