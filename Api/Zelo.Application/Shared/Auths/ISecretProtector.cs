namespace Zelo.Application.Shared.Auths;

public interface ISecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}
