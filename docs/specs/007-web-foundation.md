# Frontend foundation

User requested Next.js in `web`, centralized code-defined routes and a login based on the supplied dashboard reference. Use the Zelo name and its light surfaces, navy text, blue highlights and rounded cards. No financial data is invented after sign-in.

Next serves a single application entry; React Router owns application routes. Features own pages, contracts and services; shared UI and HTTP infrastructure remain separate. The same-origin Next API bridge stores backend access/refresh tokens in HttpOnly cookies, forwards bearer authentication and renews expired sessions. Browser state only contains user/account metadata. Auth route guards are presentation only; backend authorization remains authoritative.

Initial routes: login, registration, password recovery/reset, email confirmation and an authenticated workspace entry. Login supports TOTP challenges and recovery codes. API contracts follow `Api/Zelo/Controllers/AuthController.cs`. Backend URL defaults to localhost:5271 and is server-only. No changes to backend files are required.
