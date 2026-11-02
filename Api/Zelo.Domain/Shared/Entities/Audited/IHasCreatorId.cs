namespace Zelo.Domain.Shared.Entities.Audited;

public interface IHasCreatorId
{
    Guid CreatorId { get; }
}
