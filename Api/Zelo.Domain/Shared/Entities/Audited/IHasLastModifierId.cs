namespace Zelo.Domain.Shared.Entities.Audited;

public interface IHasLastModifierId
{
    Guid? LastModifierId { get; }
}
