namespace Zelo.Application.Shared.Context;

public interface ICurrentTenant
{
    Guid? Id { get; }
}
