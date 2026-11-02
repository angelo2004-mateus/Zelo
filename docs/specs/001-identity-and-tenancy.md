# Identity and tenancy specification

Status: The identity/tenant entities, NHibernate mappings, registration, JWT login, tenant authorization, company creation, roles, memberships, and invitations are implemented. See `003-authorization-and-invitations.md`, `005-application-services.md`, and `006-authentication-lifecycle.md`.

## Model

`Account` is the tenant. A `Company` belongs to one account, and an account can have many companies. A global `User` can belong to several accounts through `UserTenant`. Membership in an account covers all its companies.

`Role` defines a name within one tenant. `RolePermission` gives the role supported actions. Each `UserTenant` membership points to exactly one role in that tenant through `RoleId`. The same user may have different roles in different accounts. `User.UserRole` is an enum with `User` and `Admin`; `Admin` grants platform-wide access without requiring membership.

```text
User --< UserTenant >-- Account --< Company
              |
              +-- Role --< RolePermission
```

## Entities and properties

| Entity | Properties beyond inherited properties | Purpose |
| --- | --- | --- |
| `User` | `Email`, `Name`, `UserRole` enum, `Status` | Global login identity. Inherits `Id`, `CreationTime`, and `LastModifiedTime` from `AggregateRoot`. |
| `Account` | `Name`, `Status` | Group and tenant boundary. Inherits the four audit properties from `AuditedAggregateRoot`. |
| `UserTenant` | `UserId`, `TenantId`, `RoleId`, `Status`, `JoinTime` | User membership in one account and its single tenant role. Inherits the four audit properties. |
| `Company` | `TenantId`, `LegalName`, `TradeName?`, `FederalTaxNumber`, `Status` | Legal company within one account. Inherits the four audit properties. |
| `Role` | `Id`, `TenantId`, `Name` | Tenant-owned role definition. It has no status, description, permission list, or audit fields. |
| `RolePermission` | `TenantId`, `RoleId`, `Permission` | Tenant-owned grant of one supported permission code to a role. Inherits `Id`, `CreationTime`, and `LastModifiedTime` from `Entity`. |

The four audit properties are `CreationTime`, `LastModifiedTime?`, `CreatorId`, and `LastModifierId?`. `Role` intentionally does not inherit `AuditedAggregateRoot` because its current requirement is limited to a name and identifiers.

String properties in the C# entities use `string` without initializers, following the project's entity style. Required values will be checked by application validation and database constraints before persistence. `TradeName` uses an empty string when absent.

`UserRole` names only the global enum on `User`; it is not a tenant assignment table. Tenant authorization uses `UserTenant.RoleId` and `RolePermission`.

## Access rules

1. An active user with `UserRole.Admin` can access any account and company.
2. Otherwise, the user needs an active `UserTenant` for the account with a valid tenant-owned `RoleId`.
3. Membership grants access to all companies in the tenant for actions permitted by its role. A role never limits a user to a subset of companies.
4. `UserTenant.RoleId` must reference a role owned by the same tenant.
5. A disabled user or a suspended/revoked membership grants no access.
6. Every company request must confirm `Company.TenantId` matches the account being accessed.

`Role` stores only a name; `RolePermission` grants supported actions. See `003-authorization-and-invitations.md` for the permission catalog, privilege limits, and invitation rules.

## Main flows

### Register and create an account (implemented)

1. Register the user. Normal registrations set the `User.UserRole` enum to `User`.
2. Create the account, the `Owner` role with all catalog permissions, three starter roles, and an active `UserTenant` for its creator with `RoleId` pointing to `Owner`, in one transaction.
3. Preserve the account if its creator later leaves. The application must prevent removing its last active owner.

### Create a company

1. Authorize the user for the account and `company.manage`.
2. Create the company with `TenantId = Account.Id`.
3. Existing account members can access the new company according to their roles; no company membership is created.

### Add a user or assign a role

1. Add or reactivate the `UserTenant` membership for the account through an accepted invitation, or update an existing membership through an authorized management action.
2. Set the membership's `RoleId` to one role owned by the same tenant. Changing roles updates that property; no separate assignment row exists.
3. Revoking membership removes access to the account and all its companies, without affecting other accounts.

### Create a role

An actor with `member.manage` creates a `Role` with a tenant-unique name and grants supported permission codes through `RolePermission`, limited to permissions the actor already has. The application reserves and protects the initial `Owner` role. See `003-authorization-and-invitations.md` for grant limits.

Email invitations and token delivery are specified in `003-authorization-and-invitations.md`.

## Database rules

- Unique user email under case-insensitive comparison; `User` does not store `NormalizedEmail`.
- Unique `(TenantId, UserId)` for `UserTenant`.
- Unique role name per tenant under case-insensitive comparison.
- Unique `(TenantId, RoleId, Permission)` for `RolePermission`; the role must belong to the same tenant.
- A tenant-safe foreign key must ensure `UserTenant.RoleId` points to a `Role` in the same tenant.
- `TenantId` references `Account.Id`. Company-owned financial data will also carry `CompanyId`, with matching-tenant constraints.
- `FederalTaxNumber` stores the Brazilian CNPJ as text and must allow alphanumeric values. The first 12 characters may be ASCII letters or digits; the last two are numeric check digits. See the [Receita Federal's technical document](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj/manual-dv-cnpj.pdf/%40%40download/file).
- `CreationTime`, `LastModifiedTime`, and `JoinTime` contain UTC instants. Business dates use date-only values.

## Implementation notes

- The entity classes currently contain only virtual auto-properties and public empty constructors. They do not normalize, validate, or enforce the rules above.
- There are no direct property initializers. Entity strings are declared as `string`; the domain project suppresses the constructor initialization warning because NHibernate and application services populate them after construction.
- The enum `Users.UserRole` controls global access; `UserTenant.RoleId` selects the tenant role.
- NHibernate mappings, database constraints, registration/login, tenant authorization, and invitations are implemented. The auth smoke test exercises them together.
