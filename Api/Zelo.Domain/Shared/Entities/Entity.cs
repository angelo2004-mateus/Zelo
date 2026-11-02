namespace Zelo.Domain.Shared.Entities;

public abstract class Entity : IEntity
{
    public virtual Guid Id { get; set; }
    public virtual DateTime CreationTime { get; set; }
    public virtual DateTime? LastModifiedTime { get; set; }

    public Entity() { }
}
