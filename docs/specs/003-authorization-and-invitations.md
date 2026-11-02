# Authorization and invitations specification

Status: The catalog, starter roles, tenant authorization, role and member administration, invitations, and company permission enforcement are implemented.

## Authorization model

`Account.Id` is the tenant ID. An active `User` with global enum `UserRole.Admin` can act in any account without membership. Other users require an active `Account` and an active `UserTenant` whose `RoleId` points to one role in that account. Their effective permissions are the permissions granted to that role. A role name alone never authorizes an action. No role restricts access to a subset of companies; every company operation also checks `Company.TenantId` against the selected account.

`RolePermission` lives in `Roles/RolePermission.cs` and implements `IMultiTenant`. It has inherited `Id`, `CreationTime`, and `LastModifiedTime` plus `TenantId`, `RoleId`, and `Permission` (`string`). It has a public empty constructor after its virtual properties. `Permission` stores a stable code from an application-defined catalog, not a user-entered label. `Role` remains limited to `Id`, `TenantId`, and `Name`. A user-created role can combine allowed codes without adding columns to `Role`.

An active membership may view its account and companies without an additional permission. The initial catalog controls changes and access to financial data:

| Code | Allows |
| --- | --- |
| `account.manage` | Change account settings. |
| `company.manage` | Create, edit, and archive companies. |
| `member.manage` | View and manage members, invitations, non-Owner roles, and role assignments. |
| `finance.read` | View financial records, including cards, transactions, and expenses when those features exist. |
| `finance.export` | Export financial data when export exists. |
| `finance.manage` | Create and change financial records when those features exist. |

Finance permissions are reserved in the initial catalog, but grant no action until the corresponding finance use cases and authorization checks exist. More specific financial permissions may be added as those use cases are designed. An authorization check must reject an unknown code. Permission codes are stable identifiers: rename or removal needs a data migration, not an in-place string change.

The account-creation flow seeds an immutable `Owner` role with every catalog permission and sets the creator's `UserTenant.RoleId` to that role. Only an active Owner in that tenant or an active global `UserRole.Admin` may make another member an Owner or change an Owner to another role. The last active Owner cannot be removed, suspended, revoked, or changed to another role. The `Owner` role itself cannot be renamed, deleted, or edited. When new tenant permissions are introduced, the migration/bootstrap process adds them to every Owner role.

The same transaction seeds three editable starter roles. `Administrator` gets every catalog permission except `account.manage` and cannot change Owner memberships. `Accountant` gets `finance.read` and `finance.export`. `ReadOnly` gets `finance.read`. All active members can view their account and companies; these starter roles do not need separate read grants for those resources. Only the creator receives a membership automatically. The starter roles are ordinary tenant roles after creation; their names do not have privileged meaning and their permissions can be edited by an authorized actor.

Authorization decisions use the authenticated `User.Id`, the requested `TenantId`, current user/account/membership status, `UserTenant.RoleId`, and current `RolePermission` rows. Client-supplied role names or permission claims are not authoritative. Membership in one account never grants access to another. A disabled user has no access, including global admin access. An inactive account blocks tenant operations other than explicit platform administration.

To prevent privilege escalation, a tenant actor needs `member.manage` to create or change a non-Owner role and may add only permissions they already hold. The same permission is required to change a membership's role; the actor may select only a role whose entire permission set is contained in their own effective permissions. Owner changes have the separate Owner rule above. These checks and the write happen in one transaction using current permission data; global admins bypass the tenant permission ceiling. Deleting a role referenced by memberships is rejected until those memberships are moved to another role or revoked and removed according to retention rules.

## Invitation model and flow

`Invitation` belongs to one tenant and stores `Id`, `TenantId`, `Email`, `RoleId`, `InvitedByUserId`, `Status`, `ExpirationTime`, `CreationTime`, `AcceptedTime`, and a hash of a single-use secret. `RoleId` points to one role owned by that tenant; no invitation-role join table is needed. `Email` is compared case-insensitively; the existing `User` entity does not gain a normalized-email property.

1. An inviter needs `member.manage` in the account. The invitation specifies exactly one role from that tenant. The same privilege-ceiling rule used for a direct role change applies; only an Owner or global admin can invite someone as Owner. An invitation for an existing active member is rejected; membership changes use the member-management flow.
2. The application creates one pending invitation per `(TenantId, Email)` under case-insensitive comparison, with a seven-day expiration. Before creating a new invitation, an expired pending record is marked expired. Resending rotates the secret and invalidates the prior link. Revocation and expiration make the link unusable. Only a digest is stored; the raw secret is sent once in the invitation email. Email delivery occurs after the transaction commits; the response reports `emailSent`. A failed delivery leaves a pending invitation that an authorized member can resend. A durable email queue remains future reliability work.
3. The recipient opens the link, signs in or registers, and accepts while authenticated as a user whose email matches the invitation email case-insensitively. The link serves as proof of access to that mailbox; accepting while signed in under a different email is rejected.
4. Acceptance checks the invitation again, confirms that its role still exists in the same tenant, and rechecks that the inviter is still authorized to grant it. It creates or reactivates `UserTenant`, sets its `RoleId` to the invited role, and marks the invitation accepted in one transaction. A suspended membership is not silently reactivated by invitation; an authorized member-management action is required. A repeated or concurrent acceptance cannot create duplicate memberships.
5. Revoking membership immediately ends access to the account and all its companies. It does not delete the global user or affect other accounts. Any later reinvitation requires a new accepted invitation.

The invitation secret must be unpredictable, short-lived, and single-use. API responses should not reveal whether an email belongs to an existing global user. Invitation delivery, token validation, and acceptance will be implemented in the application/infrastructure layers; no email or HTTP dependency belongs in the domain.

## Persistence and validation rules

- Unique `(TenantId, RoleId, Permission)` for `RolePermission`.
- Composite foreign keys or equivalent constraints ensure `RolePermission.RoleId`, `UserTenant.RoleId`, and `Invitation.RoleId` all reference roles in the same tenant.
- Application validation rejects unknown permission codes and empty roles when creating or updating a usable role. Database constraints enforce nonempty permission strings and required identifiers.
- Unique pending invitation per tenant and case-insensitive email, plus unique membership `(TenantId, UserId)`.
- Changes to memberships and role permissions invalidate any cached authorization decisions for the affected tenant/user. Until invalidation is implemented, authorization reads current database state.
- Invitation times are UTC instants. The seven-day validity and expiry are checked using an application-provided UTC clock.

## API scope for this implementation

`POST /api/auth/register-user` creates only a global user and password credential, then sends confirmation email. It supports recipients who do not want to create a new account. The existing `register` endpoint still creates the first account.

Public registration always creates `UserRole.User`. A trusted operator must provision `UserRole.Admin` out of band; no public endpoint can grant platform administration. An active platform admin can access all accounts without a `UserTenant`, including suspended accounts, and loses that access immediately if the global user is disabled.

Authenticated account endpoints use the JWT subject to load current authorization data from PostgreSQL. `GET /api/accounts/{tenantId}` checks account visibility. `GET`, `POST`, and `PUT /api/accounts/{tenantId}/roles` require `member.manage` to list, create, and edit non-Owner roles. `GET /api/accounts/{tenantId}/members`, `PATCH /api/accounts/{tenantId}/members/{userId}/role`, `PATCH /api/accounts/{tenantId}/members/{userId}/status`, and `DELETE /api/accounts/{tenantId}/members/{userId}` require `member.manage`. Status updates allow suspension/reactivation, while revoked memberships require a new invitation. These operations enforce the Owner and permission-ceiling rules above. `GET /api/accounts/{tenantId}/companies` is available to any active member; `POST /api/accounts/{tenantId}/companies` requires `company.manage`.

`POST /api/accounts/{tenantId}/invitations` creates or resends a pending invitation and delivers its token through the auth email sender. `POST /api/accounts/{tenantId}/invitations/{invitationId}/revoke` revokes one. `POST /api/invitations/accept` requires an authenticated, confirmed user whose email matches the invitation and an opaque token. Acceptance rechecks the inviter and role, then creates or reactivates membership in one transaction. A rejected or expired invitation never grants membership. `GET /api/accounts` reflects accepted and revoked memberships immediately.

Invitation links are represented as opaque tokens in email; the client supplies them to the accept endpoint. The API does not expose raw invitation secrets in list or create responses. A production email delivery queue is a later reliability improvement; SMTP failure leaves a pending invitation that can be resent.
