namespace Zelo.Application.Shared.Auths;

public class AuthEmailDeliveryException : Exception
{
    public AuthEmailDeliveryException(Exception innerException)
        : base("Authentication email delivery failed.", innerException)
    {
    }
}
