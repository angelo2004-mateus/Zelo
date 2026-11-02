# Zelo project rules

## Workflow

- Discuss and record domain decisions in `docs/specs/` before implementing the affected feature.
- When the user asks for a specification before implementation, deliver only the specification. Wait for explicit approval of that specification before implementing it.
- Keep specifications current when a domain rule changes. Do not treat a draft as an approved implementation contract.
- Implement only the scope requested for the current task. Do not create unrelated modules or entities in advance.
- Always use braces for `if`, `else`, and other control-flow blocks. Prefer block-bodied lambdas and readable line breaks; keep expression-bodied lambdas only where an API requires an expression tree, such as NHibernate selectors and query predicates.
- Keep class declarations (including generic types), constructors, method signatures, and ordinary calls inline. Break a line only when the full expression becomes genuinely difficult to read; do not wrap after each parameter or generic argument.
- For an API resource, use Controller -> AppService -> repository. Keep NHibernate queries in Infrastructure repositories; application services coordinate use cases and map results to DTOs.
- Put entity-specific repository interfaces in that entity's `Zelo.Domain/<Subject>/` folder. Keep the generic `IRepository<TEntity, TId>` in `Zelo.Domain/Shared/Repositories/`; Infrastructure implements these contracts.
- Keep the domain independent of ASP.NET, NHibernate, PostgreSQL, and financial-data providers.
- Keep domain entities as property-only classes with a public parameterless constructor unless an approved spec explicitly adds behavior. Aggregate roots have the `DomainEvents`, `AddEvent`, and `PullEvents` behavior defined in spec 009; ordinary entities do not gain events automatically.
- Do not add property initializers such as `= null!` or default collections. Use `string` rather than `string?` on domain entity properties; required values are validated before persistence.

## Naming and files

- One public type per file. The file name matches the public type exactly.
- Place the parameterless constructor after all properties in every class.
- Group files by domain subject. Within a subject folder, use the singular subject name as the prefix when it reads naturally: `Companies/Company.cs`, `UserTenants/UserTenant.cs`.
- Keep non-API projects, including `Zelo.Domain`, `Zelo.Application`, and `Zelo.Infrastructure`, in the solution's `Core` folder. Use a `Shared/` folder inside a layer for code reusable within that layer; do not introduce a separate `Zelo.Frame` project. Put application DTOs under `Zelo.Application/Model/<Subject>/`. Put NHibernate maps under `Zelo.Infrastructure/Data/Mappings/<DomainFolder>/`, one map per entity, named `<Entity>Map.cs` (for example, `Users/UserMap.cs` and `Roles/RolePermissionMap.cs`).
- Let NHibernate infer column names from properties; PostgreSQL stores the unquoted identifiers in lowercase without separators (`CreationTime` -> `creationtime`). Use lowercase names without underscores for tables and schema identifiers. Reuse `EntityClassMapping<TEntity>` for ID/timestamps and `AuditedAggregateClassMapping<TEntity>` for audited actor IDs.
- Keep canonical names for shared abstractions in `Shared/Entities/`, such as `IEntity.cs`, `Entity.cs`, and `AggregateRoot.cs`.
- Put audit timestamp and actor interfaces in `Shared/Entities/Audited/` and use the matching `.Audited` namespace.
- Keep AutoMapper entity/DTO maps together in `Zelo.Application/Mapping/ApplicationMappingProfile.cs`. Use a shared map helper for reverse maps and protect only matching identity/scope fields; do not list ignores for entity properties absent from a DTO.
- Use the spelling `Membership` and `AggregateRoot`.
- Enum members use implicit sequential values. Do not write `= 0` for the first member or explicit values unless persisted compatibility requires them.
- Persisted enums must not be reordered or renumbered after data exists. Prefer string mappings or an explicit migration when changing them.

## Domain and tenant boundaries

- `Account` is the tenant. A `Company` belongs to exactly one `Account`; there is no separate `Tenant` entity.
- `User` is global. `User.UserRole` is the `Zelo.Domain.Users.UserRole` enum (`User`, `Admin`); `Admin` grants platform administration without a `UserTenant`.
- `UserTenant` connects a user to an account and carries one tenant-owned `RoleId`. An active membership grants access to all companies in that account, subject to that role's permitted actions.
- `Role` is a tenant-owned ordinary entity with only `Id`, `TenantId`, and `Name`; it is not an aggregate root and has no domain events. `RolePermission` links a role to a supported permission code without adding properties to `Role`. There is no separate tenant role-assignment entity; `Zelo.Domain.Users.UserRole` is only the global `User` enum.
- Do not introduce company-level memberships unless a new approved requirement calls for them.
- Tenant-owned data carries `TenantId`, which references `Account.Id`; company-owned data carries both `TenantId` and `CompanyId`. Enforce ownership with authorization checks and database constraints.
- `IMultiTenant` exposes `TenantId`. Scope interfaces do not authorize access on their own.
- Generic CRUD obtains the authenticated user and route tenant through `ICurrentUser` and `ICurrentTenant`; controllers pass only resource IDs and DTOs. `ICurrentCompany` exposes a company route ID when one exists. The NHibernate tenant filter is an isolation layer, not a membership authorization check.
- Keep provider payloads and identifiers at the integration boundary. Domain entities own their IDs and business rules.

## Time and persistence

- Use UTC for persisted instants. Name time properties with `Time` rather than an `AtUtc` suffix; examples are `CreationTime`, `LastModifiedTime`, and `JoinTime`. Keep business dates (such as due dates) as dates.
- Audit interfaces are independent: `IHasCreationTime`, `IHasLastModifiedTime`, `IHasCreatorId`, and `IHasLastModifierId`. `AuditedAggregateRoot` implements all four directly.
- Stamp audit timestamps and authenticated actor IDs at the NHibernate session persistence boundary. Application services do not set audit fields, except an explicit creator ID in unauthenticated bootstrap flows where there is no current user.
- Use English property names. A company's Brazilian tax identifier is `FederalTaxNumber`.
- Keep public parameterless constructors and virtual auto-properties on NHibernate-mapped entities.
- Add a case-insensitive unique email constraint and a unique `(TenantId, UserId)` constraint on `UserTenant`.
- Keep role names unique per tenant and require `UserTenant.RoleId` to reference a role in the same tenant.
- Keep permission codes in an application-defined catalog. Enforce unique `(TenantId, RoleId, Permission)` and same-tenant role references for `RolePermission`.
- Start with six tenant permissions: `account.manage`, `company.manage`, `member.manage`, `finance.read`, `finance.manage`, and `finance.export`. Active membership itself grants account/company viewing. Expand the catalog only for a concrete use case.
