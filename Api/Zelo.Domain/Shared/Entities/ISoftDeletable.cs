namespace Zelo.Domain.Shared.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletionTime { get; set; }
    Guid? DeleterId { get; set; }
}
