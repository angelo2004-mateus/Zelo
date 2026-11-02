# Web authentication lifecycle

Requested: complete frontend auth using the existing API, preserve the blue theme, format code and run ESLint.

Dedicated feature pages cover registration (new account or user only), confirmation and resend, password recovery/reset, login, authenticator/recovery challenge, security settings, and one-time recovery-code presentation. Forms validate email, required names and password confirmation; the API remains authoritative. Delivery failures after successful registration show a resend action. Confirmation/reset accept email links or manually pasted tokens because the current email sender delivers plain token text.

TOTP setup requires the current password. QR codes are generated locally from the returned otpauth URI. Activation and deactivation revoke all sessions. Recovery codes are held in memory only, displayed once with copy/download and an acknowledgement before returning to login. The backend does not expose TOTP status; settings offer explicit enrollment and disabling without pretending to know its status.

The Next bridge keeps tokens in HttpOnly cookies with backend expiration times. It validates identity against `auth/me`, fetches account access from the API and refreshes on 401. User profile metadata is only presentation, not authorization. Mutation endpoints require same-origin JSON requests. Successful TOTP changes and password reset clear local cookies. Browser session failures distinguish expiration from service unavailability; protected routes preserve a safe internal return path. No tokens/passwords/secrets are logged or put in localStorage.

Verification includes Prettier, ESLint, TypeScript, Vitest form validation tests, production build and a repeatable HTTP smoke test of the frontend bridge with the real .NET API and a disposable PostgreSQL database. Only `web` and frontend specs are changed.

Local integration: `npm run dev:api` starts the actual API on 127.0.0.1:5271, prepares the dedicated persistent `zelo_dev` database and stores its development signing key and email outbox under ignored `web/.local/`. Frontend `.env.local` points to this API. Existing unrelated databases are not modified.
