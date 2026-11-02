using NHibernate;
using NHibernate.Type;
using Zelo.Application.Shared.Context;
using Zelo.Domain.Shared.Entities;

namespace Zelo.Infrastructure.Shared.Data;

public sealed class AuditInterceptor : EmptyInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override bool OnSave(object entity, object id, object[] state, string[] propertyNames, IType[] types)
    {
        if (entity is not Entity tracked)
        {
            return false;
        }

        tracked.CreationTime = DateTime.UtcNow;
        SetState(state, propertyNames, nameof(Entity.CreationTime), tracked.CreationTime);
        if (entity is AuditedAggregateRoot audited && _currentUser.Id is Guid userId)
        {
            audited.CreatorId = userId;
            SetState(state, propertyNames, nameof(AuditedAggregateRoot.CreatorId), userId);
        }

        return true;
    }

    public override bool OnFlushDirty(object entity, object id, object[] currentState, object[] previousState, string[] propertyNames, IType[] types)
    {
        if (entity is not Entity tracked)
        {
            return false;
        }

        if (entity is AuditedAggregateRoot && _currentUser.Id is null)
        {
            return false;
        }

        tracked.LastModifiedTime = DateTime.UtcNow;
        SetState(currentState, propertyNames, nameof(Entity.LastModifiedTime), tracked.LastModifiedTime);
        if (entity is AuditedAggregateRoot audited && _currentUser.Id is Guid userId)
        {
            audited.LastModifierId = userId;
            SetState(currentState, propertyNames, nameof(AuditedAggregateRoot.LastModifierId), userId);
        }

        return true;
    }

    private static void SetState(object[] state, string[] propertyNames, string name, object value)
    {
        var index = Array.IndexOf(propertyNames, name);
        if (index >= 0)
        {
            state[index] = value;
        }
    }
}
