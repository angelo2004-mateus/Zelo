# Identity persistence specification

Status: NHibernate mappings and PostgreSQL migrations are implemented for identity, tenancy, password credentials, authentication sessions/tokens, TOTP, and invitations. Finance entities require later migrations.

## Boundary

`Zelo.Infrastructure` sits beside `Zelo.Domain` in the solution's `Core` folder, references the domain project, and owns NHibernate mappings and PostgreSQL configuration. Domain entities remain free of persistence attributes and behavior. One `ISessionFactory` is shared by the process; each application operation will open its own `ISession` and explicit transaction. Authorization still has to scope queries by the requested `TenantId`; a mapped `IMultiTenant` property is not an automatic filter.

The initial migration is `Zelo.Infrastructure/Data/Migrations/0001_identity.sql`; authentication adds `0002_auth.sql`. Apply both explicitly before the API uses the schema. Runtime schema generation or automatic destructive updates are not used. IDs are application-generated UUIDs. Enum values are stored as integers and must not be reordered after migration. UTC instants use PostgreSQL `timestamptz`, with UTC values supplied by the application.

## Tables

| Table | Domain type | Important constraints |
| --- | --- | --- |
| `users` | `User` | Primary key; case-insensitive unique email; valid global role/status. |
| `accounts` | `Account` | Primary key; creator and optional last modifier reference users. |
| `roles` | `Role` | Primary key; `tenantid` references accounts; tenant-unique case-insensitive name; unique `(tenantid, id)` for tenant-safe references. |
| `rolepermissions` | `RolePermission` | Primary key; unique `(tenantid, roleid, permission)`; composite FK to a role in the same tenant; nonblank permission. |
| `usertenants` | `UserTenant` | Primary key; unique `(tenantid, userid)`; composite FK from `(tenantid, roleid)` to roles; audit users and status. |
| `companies` | `Company` | Primary key; account FK; audit users; nonblank legal name and federal tax number; company status. |
| `userpasswordcredentials` | `UserPasswordCredential` | Added in migration 0002; unique user FK; nonblank password hash. |

The migration enforces ownership and uniqueness. Application validation enforces email/CNPJ syntax, the supported permission catalog, the protected Owner role, and last-owner rules. No invitation table is created until the invitation use case is implemented.

## Mapping and use

Maps live under `Zelo.Infrastructure/Data/Mappings/`, mirroring domain folders: `Accounts/`, `Companies/`, `Invitations/`, `Roles/`, `Users/`, and `UserTenants/`. Each map contains only its entity-specific properties. `EntityClassMapping<TEntity>` maps `Id`, `CreationTime`, and `LastModifiedTime`; `AuditedAggregateClassMapping<TEntity>` inherits it and maps `CreatorId` and `LastModifierId`. `Role` remains directly mapped because it does not derive from `Entity`.

No map sets individual column names. NHibernate uses property names as unquoted SQL identifiers, which PostgreSQL folds to lowercase without separators: `CreationTime` becomes `creationtime`, `TenantId` becomes `tenantid`. Table and other schema identifiers follow the same lowercase, separator-free convention, including `rolepermissions` and `usertenants`. Role and membership associations remain scalar UUID properties rather than lazy navigation collections. The infrastructure exposes a configured session factory, repositories, a transaction boundary, password hashing, JWT signing, TOTP secret protection, and email delivery. Tenant authorization and invitation persistence are implemented; a durable email queue remains future reliability work.
