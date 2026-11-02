# Application and authentication specification

Status: Initial registration, JWT login, and accessible-account listing are implemented. The later authentication lifecycle is specified in [006-authentication-lifecycle.md](006-authentication-lifecycle.md). Account management and invitations remain outside this slice.

## Layer layout

All non-API projects remain in the solution's `Core` folder. There is no separate framework project. Reusable code belongs under `Shared` in its owning layer.

```text
Zelo.Application/
  Contract/IUserAppService.cs
  Contract/IAccountAppService.cs
  Model/Auths/...
  Model/Users/...
  Model/Accounts/...
  Services/UserAppService.cs
  Services/AccountAppService.cs
  Services/ZeloJwtBearerAuthService.cs
  Shared/Auths/...                 generic authentication and JWT service base
  Shared/Data/IUnitOfWork.cs       application transaction contract
Zelo.Domain/
  Accounts/IAccountRepository.cs
  Accounts/AccountAccess.cs
  Shared/Repositories/IRepository.cs
  Users/UserPasswordCredential.cs
Zelo.Infrastructure/
  Data/Mappings/Users/UserPasswordCredentialMap.cs
  Data/Repositories/...
  Data/Repositories/Accounts/AccountRepository.cs
  Data/Stores/Users/...
  Shared/Auths/...                 password hash and JWT signing implementations
Zelo.Web.Api/Controllers/AuthController.cs
Zelo.Web.Api/Controllers/AccountsController.cs
```

The application project references only the domain. Infrastructure implements application ports with NHibernate, password hashing, and JWT signing. The Web API composes them and validates bearer tokens. Account requests follow Controller -> AppService -> repository. Repository interfaces belong to the Domain: the generic contract is under `Shared/Repositories`, while `IAccountRepository` and its access projection are under `Accounts`. The infrastructure repository returns the domain access projection; the app service maps it to `AccountAccessDto`.

## Authentication design

`AuthService<TUser, TAuthResult>` verifies a user identifier, password hash, confirmed email, active state, and lockout state, returning one generic invalid-credentials outcome on failure. It calls a protected virtual success method only after primary authentication is complete. `JwtBearerAuthService<TUser, TAuthResult>` overrides that method to create a session and issue a JWT through interfaces. `ZeloJwtBearerAuthService` intercepts enabled TOTP and otherwise attaches the user's active account memberships. The controller depends on the auth-service contract, not the concrete hierarchy.

`UserPasswordCredential` stores one password hash for a user, separate from the global user profile. The generic auth flow reads an authentication record through a user-store port and hashes/verifies passwords through a password-hasher port. It does not use reflection attributes on domain entities. The user ID is the JWT subject. Tenant IDs, role permissions, and account lists are not treated as immutable token grants; tenant authorization reads current membership and role data for the requested account. The login response may include an account list for UI selection.

JWT signing uses a configured issuer, audience, expiration, and secret outside source control. The API validates signature, issuer, audience, and lifetime. The second-factor flow stops before a session and JWT are issued.
Bearer validation also rechecks that the referenced global user is active and confirmed and that its session is active, so disabling a user or revoking a session invalidates access without waiting for token expiry.

## Registration and endpoints

`POST /api/auth/register` receives email, password, user name, and first account name. `UserAppService.RegisterAsync` validates and normalizes the input and, within one transaction, creates the `User`, `UserPasswordCredential`, `Account`, tenant roles and their permissions, and an active `UserTenant` assigning the creator to `Owner`. The new user's email is unconfirmed until the email token is consumed. Registration cannot assign global `UserRole.Admin`. An email already used by a global user returns a conflict. The response contains safe user/account DTOs and no password hash.

`POST /api/auth/login` accepts email and password. It returns a bearer access token, refresh token, safe user DTO, and accounts accessible to that user, or one generic unauthorized response. When TOTP is enabled it returns only a challenge, completed via `/api/auth/totp/verify`. The Zelo JWT specialization obtains the account list through `IAccountAppService`, which uses `IAccountRepository`. `GET /api/accounts` exposes the same accessible-account list to an authenticated user. Ordinary users see active accounts through active `UserTenant` memberships. A global `UserRole.Admin` sees all accounts, including suspended accounts, without a tenant membership; those entries are marked as platform-admin access and carry no tenant role. `GET /api/auth/me` confirms bearer validation and returns the authenticated user ID. Creating additional accounts as an existing user will be a later account use case, not a public registration.

Application DTOs are grouped in `Model/Auths`, `Model/Users`, and `Model/Accounts`. Controllers do not return domain entities. The permission catalog and starter-role grants follow `003-authorization-and-invitations.md`: `Owner` has all six grants; `Administrator` all except `account.manage`; `Accountant` has `finance.read` and `finance.export`; `ReadOnly` has `finance.read`.

## Persistence and scope

The password credential requires a forward-only migration with a unique user reference and a matching NHibernate mapping. The registration transaction commits all rows together. Existing identity tables and naming conventions stay unchanged. Invitations and tenant permission enforcement remain separate.

To run authentication, apply `0001_identity.sql`, `0002_auth.sql`, and `0003_authlifecycle.sql` to PostgreSQL. Set `ConnectionStrings__Zelo` to the PostgreSQL connection string and `Jwt__SigningKey` to a secret of at least 32 UTF-8 bytes. `Jwt:Issuer`, `Jwt:Audience`, and `Jwt:ExpirationMinutes` are configured in API settings. No signing key is committed to the project.
