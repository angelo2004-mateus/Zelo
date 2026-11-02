# Domain foundations specification

Status: Initial shared contracts implemented; details remain open for review before business entities are added.

## Core contracts

| Type | Contract | Purpose |
| --- | --- | --- |
| `IEntity` | `Guid Id` | Stable application-owned identity. |
| `IHasCreationTime` | `DateTime CreationTime` | UTC creation instant. |
| `IHasLastModifiedTime` | `DateTime? LastModifiedTime` | UTC last-change instant; null before the first modification. |
| `IHasCreatorId` | `Guid CreatorId` | Records the user who created an entity. Independent interface. |
| `IHasLastModifierId` | `Guid? LastModifierId` | Records the user responsible for the most recent modification. Independent interface. |
| `IAggregateRoot` | Extends `IEntity` | Marks a consistency boundary and exposes domain events, `AddEvent`, and `PullEvents`. |
| `Entity` | Implements `IEntity`; has `Id`, `CreationTime`, and `LastModifiedTime` properties | Base properties for entity identity and timestamps. |
| `AggregateRoot` | Extends `Entity`, implements `IAggregateRoot` | Base for aggregate roots with transient event storage. |
| `AuditedAggregateRoot` | Extends `AggregateRoot`; implements all four audit interfaces directly | Adds `CreatorId` and `LastModifierId`; time properties come from `Entity`. |
| `IMultiTenant` | `Guid TenantId` | Marks data owned by one tenant (`Account.Id`). |
| `ICompanyScoped` | Extends `IMultiTenant`; adds `Guid CompanyId` | Marks data owned by one company inside one tenant. |

Use the canonical names above. `IEntity.cs`, `Entity.cs`, `IAggregateRoot.cs`, `AggregateRoot.cs`, `AuditedAggregateRoot.cs`, `IMultiTenant.cs`, and `ICompanyScoped.cs` go in `Shared/Entities/`. `IHasCreationTime.cs`, `IHasLastModifiedTime.cs`, `IHasCreatorId.cs`, and `IHasLastModifierId.cs` go in `Shared/Entities/Audited/` with the matching `.Audited` namespace. None of the four audit interfaces inherits another audit interface.

## Behavioral rules

- Domain entities have virtual auto-properties and public parameterless constructors. Aggregate roots additionally have the event collection methods implemented by spec 009; ordinary entities such as `Role` remain without event methods. Business behavior beyond this event plumbing requires its own approved specification.
- IDs and `CreationTime` are assigned by the application when an entity is created. `LastModifiedTime` is set by the application when it modifies an entity. Both times contain UTC values even though their names omit the `Utc` suffix.
- Actor auditing is opt-in through `AuditedAggregateRoot`. `Entity` provides timestamp properties without implementing audit interfaces or requiring a user actor, because provider sync and other background operations may not have one.
- The application supplies `CreatorId` when creating an audited aggregate. It supplies `LastModifierId` together with `LastModifiedTime` when modifying it; the latter two are null before the first modification.
- Application-supplied clock services should provide UTC instants so creation and modification can be tested deterministically.
- A date such as a payment due date uses a date-only value. It is not represented by `CreationTime` or another instant.
- NHibernate-mapped entities expose virtual auto-properties followed by a public parameterless constructor, following the project's current data-model style.
- `IMultiTenant` and `ICompanyScoped` describe data ownership. They do not authorize a request or automatically scope a database query.
- Aggregate boundaries are selected by business invariants. An account does not load every company, transaction, and expense as one aggregate.

## Initial aggregate candidates

`User`, `Account`, `UserTenant`, `Company`, and `Invitation` are aggregate roots in the current implementation. `Role` is a regular tenant-owned entity in the account's authorization context, and `RolePermission` is another regular entity; neither has its own domain-event list. A role's permissions and the Owner invariants are managed through account-specific use cases rather than generic Role CRUD. `UserTenant.RoleId` selects one tenant-owned role; `Zelo.Domain.Users.UserRole` is the separate global enum. `UserTenant` uniqueness, same-tenant role references, and last-owner protection require application/database coordination and database constraints.
