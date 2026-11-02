namespace Zelo.Domain.Shared.Entities.Audited;

public interface IHasLastModifiedTime
{
    DateTime? LastModifiedTime { get; }
}
