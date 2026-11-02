# Authentication lifecycle

Status: Implemented and verified by the disposable-database smoke test.

## Boundaries

The global `User` authenticates once and can then select any account allowed by current membership. Authentication does not embed tenant permissions in a JWT. Invitations and tenant authorization remain separate use cases.

`AuthService<TUser, TAuthResult>` remains the password verification base. `JwtBearerAuthService` issues a session-backed JWT. `ZeloJwtBearerAuthService` adds account data and intercepts a successful password check when TOTP is enabled. Web controllers call application services; NHibernate queries and mutations live in infrastructure stores.

## Flows

- Registration creates the user and first account in one transaction. The user starts with an unconfirmed email. A random, one-use confirmation token is sent after commit. Resending rotates the token. The registration response reports `confirmationEmailSent`; if delivery fails, the user can request a resend without losing the registered account. Existing users are marked confirmed by the migration to preserve their existing access.
- Confirmation and resend accept opaque tokens or an email address. Resend and forgotten-password responses do not disclose whether an email exists, including when the email sender fails. A new password reset request invalidates older reset tokens. Password reset consumes a one-use token, changes the password hash, and revokes all sessions.
- Login requires an active, confirmed user. Repeated failures lock the credential temporarily. When TOTP is enabled, a correct password returns a short-lived challenge; no bearer token is issued until the code is verified.
- A successful authentication creates a server-side session with a hashed refresh token. Refresh rotates that token. Logout revokes its session; password reset and TOTP changes revoke all sessions. Every bearer request validates the session and user state so revocation takes effect before JWT expiry. A TOTP challenge expires after five minutes or five failed attempts.
- TOTP enrollment requires an authenticated session and password. The setup secret is returned only at enrollment and is encrypted at rest. A valid TOTP code activates it and returns eight one-use recovery codes. Subsequent sign-in requires TOTP or a recovery code. A TOTP code may be used only once per time step. Disabling TOTP requires a valid password and a fresh TOTP or recovery code; disabling consumes all remaining recovery codes.
- API rate limits protect authentication endpoints. Durable per-user password and TOTP lockouts complement the IP limit. The IP limiter is process-local; a multi-instance deployment should also enforce a shared limit at the edge.

## Storage and delivery

`UserAuthToken` holds SHA-256 token hashes, purpose, expiration, and consumption time, including recovery codes. `UserAuthSession` holds the hashed refresh token and revocation time. `UserTotpCredential` holds the encrypted TOTP secret, enablement time, and last accepted step. All timestamps are UTC. No plaintext token, password, or TOTP secret is logged or persisted.

`IAuthEmailSender` is a port. The `Auth:EmailDelivery` setting selects `File` or `Smtp`: development defaults to a local pickup directory for a full test flow, while production defaults to SMTP. Development can explicitly select SMTP to test delivery through a local virtual inbox such as Mailpit. The pickup directory is local and outside the HTTP surface. SMTP startup requires its host and sender address; production also requires a TOTP encryption key. SMTP defaults to STARTTLS; an unencrypted connection is allowed only in development to a loopback SMTP host. Email delivery failures after registration can be recovered through resend.

New endpoints under `/api/auth`: `confirm-email`, `resend-confirmation`, `forgot-password`, `reset-password`, `refresh`, `logout`, `totp/setup`, `totp/enable`, `totp/verify`, and `totp/disable`. Access tokens remain short-lived JWTs. Refresh tokens are opaque bearer secrets sent in request bodies; clients must store them securely.

The client flow is:

1. `register` returns `201` with safe user/account data. In development, read the confirmation token from the configured pickup directory; submit it to `confirm-email` as `{ "token": "..." }`. `resend-confirmation` and `forgot-password` accept `{ "email": "..." }` and always return `202` for valid request syntax, regardless of whether the address exists.
2. `login` accepts `{ "email": "...", "password": "..." }`. Without TOTP it returns `accessToken`, `refreshToken`, `expirationTime`, `user`, and `accounts`. With TOTP it returns `succeeded: true`, `requiresTwoFactor: true`, and `challengeToken`, with no session tokens. Submit `{ "challengeToken": "...", "code": "..." }` to `totp/verify` for the tokens. The code may be a current authenticator code or an unused recovery code.
3. `refresh` accepts `{ "refreshToken": "..." }` and replaces both tokens. Successful authentication and refresh responses include `expirationTime` for the JWT and `refreshExpirationTime` for the server-side session. `logout` uses the access token as a bearer credential and revokes that session. `reset-password` accepts `{ "token": "...", "password": "..." }` and revokes all sessions.
4. With an authenticated bearer token, `totp/setup` accepts `{ "password": "..." }` and returns a Base32 secret and an `otpauth://` URI. `totp/enable` accepts `{ "code": "123456" }` and returns eight recovery codes exactly once. `totp/disable` accepts `{ "password": "...", "code": "..." }`, where the code may be TOTP or a recovery code. Enabling or disabling TOTP revokes all existing sessions.

## Operational checks

Apply migration `0003_authlifecycle.sql` after `0002_auth.sql`. The API needs `ConnectionStrings__Zelo` and `Jwt__SigningKey` (at least 32 UTF-8 bytes). `Auth__RefreshExpirationDays` defaults to 30, and `Auth__RateLimitPerMinute` defaults to 30 per IP on one API process. Development writes email to `Auth__DevelopmentOutbox` or the system temporary `zelo-auth-outbox` directory by default. Set `Auth__EmailDelivery=Smtp` to send through SMTP in development. SMTP requires `Auth__Smtp__Host` and `Auth__Smtp__From`, and accepts `Auth__Smtp__Port` (default 587), `Auth__Smtp__Username`, `Auth__Smtp__Password`, and `Auth__Smtp__Security` (`StartTls` by default, `SslOnConnect`, or `None` only for loopback development). Production defaults to SMTP and requires `Auth__TotpEncryptionKey` (Base64-encoded 32-byte key). Keep credentials and the TOTP encryption key outside the repository; keep the encryption key stable across restarts. Development derives a stable key from the configured JWT key if `Auth__TotpEncryptionKey` is omitted.

Apply `0004_invitations.sql` after `0003_authlifecycle.sql` for invitations. Run `python3 -u Api/scripts/auth_flow_smoke.py` from the repository root for one full local test. It requires `dotnet`, `createdb`, `psql`, `dropdb`, and a reachable local PostgreSQL server; it honors `PGHOST`, `PGPORT`, `PGUSER`, and `PGPASSWORD`. The script creates and removes its own database, starts the API with test secrets, reads only its temporary local email directory, and checks registration, confirmation, password reset, lockouts, session rotation/revocation, TOTP, recovery codes, roles, invitations, membership changes, tenant isolation, and platform admin access. Never use real recipient addresses in automated tests.
