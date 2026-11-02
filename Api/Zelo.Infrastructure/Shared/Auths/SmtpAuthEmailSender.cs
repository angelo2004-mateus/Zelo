using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Zelo.Application.Shared.Auths;

namespace Zelo.Infrastructure.Shared.Auths;

public class SmtpAuthEmailSender : IAuthEmailSender
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _from;
    private readonly SmtpSecurity _security;

    public SmtpAuthEmailSender(string host, int port, string username, string password, string from, SmtpSecurity security)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _from = from;
        _security = security;
    }

    public async Task SendAsync(string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(_from));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        var socketOptions = _security switch
        {
            SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            SmtpSecurity.None => SecureSocketOptions.None,
            _ => throw new InvalidOperationException("Unsupported SMTP security mode.")
        };
        await client.ConnectAsync(_host, _port, socketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_username))
        {
            await client.AuthenticateAsync(_username, _password, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
