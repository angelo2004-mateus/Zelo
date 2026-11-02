"""Test the Next auth bridge against the real API using disposable local data.

Run `npm run build` first. Requires dotnet, PostgreSQL client tools and PostgreSQL.
The script creates its own database, API process, Next process and email directory.
"""

import base64
import getpass
import hashlib
import hmac
import json
import os
import re
import secrets
import socket
import struct
import subprocess
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WEB = ROOT / "web"


def free_port():
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


class BrowserClient:
    def __init__(self, origin):
        self.origin = origin
        self.cookies = {}
        self.last_cookie_headers = []

    def call(self, action, body=None, method=None, origin=None):
        headers = {"Content-Type": "application/json", "Origin": origin or self.origin}
        # Loopback test transport only: production Secure flags are still asserted below.
        headers["Cookie"] = "; ".join(f"{k}={v}" for k, v in self.cookies.items())
        request = urllib.request.Request(
            self.origin + "/api/auth/" + action,
            data=json.dumps(body).encode() if body is not None else None,
            method=method or ("POST" if body is not None else "GET"),
            headers=headers,
        )
        try:
            response = urllib.request.urlopen(request, timeout=20)
        except urllib.error.HTTPError as error:
            response = error
        self.last_cookie_headers = response.headers.get_all("Set-Cookie", [])
        for header in self.last_cookie_headers:
            name, value = header.split(";", 1)[0].split("=", 1)
            if "Max-Age=0" in header:
                self.cookies.pop(name, None)
            else:
                self.cookies[name] = value
        content = response.read()
        return response.status, json.loads(content) if content else None


def check(actual, expected, label):
    assert actual == expected, f"{label}: HTTP {actual}, expected {expected}"


def email_token(outbox, subject, before):
    for path in set(outbox.glob("*.txt")) - before:
        content = path.read_text(encoding="utf-8-sig")
        if f"Subject: {subject}" in content:
            match = re.search(r"token: ([A-Za-z0-9_-]+)", content)
            if match:
                return match.group(1)
    raise AssertionError("Expected test email not found")


def totp(secret):
    digest = hmac.new(
        base64.b32decode(secret), struct.pack(">Q", int(time.time()) // 30), hashlib.sha1
    ).digest()
    offset = digest[-1] & 15
    return f"{(struct.unpack('>I', digest[offset:offset + 4])[0] & 0x7fffffff) % 1000000:06d}"


def exercise(client, outbox):
    email = f"web-auth-{secrets.token_hex(6)}@example.test"
    password = secrets.token_urlsafe(18)
    before = set(outbox.glob("*.txt"))
    registration = {"name": "Web Auth Test", "accountName": "Web Test Account", "email": email, "password": password}
    status, result = client.call("register", registration)
    check(status, 201, "register")
    assert result["confirmationEmailSent"]
    check(client.call("register", registration)[0], 409, "duplicate email")
    check(client.call("login", {"email": email, "password": password})[0], 401, "unconfirmed login")
    token = email_token(outbox, "Confirm your Zelo email", before)
    check(client.call("confirm-email", {"token": token})[0], 204, "confirm email")
    check(client.call("confirm-email", {"token": token})[0], 400, "one-use confirmation")
    status, result = client.call("login", {"email": email, "password": password})
    check(status, 200, "login")
    assert "accessToken" not in result and "refreshToken" not in result
    assert all("HttpOnly" in c and "SameSite=Lax" in c and "Secure" in c for c in client.last_cookie_headers)
    check(client.call("session")[0], 200, "session")
    assert len(client.call("session")[1]["accounts"]) == 1
    print("Registration, email confirmation, login, safe cookies: passed", flush=True)

    old_refresh = client.cookies["zelo_refresh"]
    client.cookies.pop("zelo_access")
    check(client.call("session")[0], 200, "automatic refresh")
    assert client.cookies["zelo_refresh"] != old_refresh
    check(client.call("logout", {})[0], 204, "logout")
    assert not client.cookies
    check(client.call("session")[0], 401, "revoked session")
    check(client.call("logout", {})[0], 204, "logout already expired")
    print("Automatic refresh, logout and revocation: passed", flush=True)

    check(client.call("login", {"email": email, "password": password})[0], 200, "login for TOTP")
    status, setup = client.call("totp/setup", {"password": password})
    check(status, 200, "TOTP setup")
    assert setup["authenticatorUri"].startswith("otpauth://")
    status, enabled = client.call("totp/enable", {"code": totp(setup["secret"])})
    check(status, 200, "TOTP enable")
    assert len(enabled["recoveryCodes"]) == 8 and not client.cookies
    check(client.call("session")[0], 401, "TOTP activation clears session")
    status, challenge = client.call("login", {"email": email, "password": password})
    check(status, 200, "TOTP challenge")
    assert challenge["requiresTwoFactor"] and "accessToken" not in challenge and not client.cookies
    status, result = client.call("totp/verify", {"challengeToken": challenge["challengeToken"], "code": enabled["recoveryCodes"][0]})
    check(status, 200, "recovery code login")
    assert "accessToken" not in result
    check(client.call("session")[0], 200, "2FA session")
    next_challenge = client.call("login", {"email": email, "password": password})[1]
    check(client.call("totp/verify", {"challengeToken": next_challenge["challengeToken"], "code": enabled["recoveryCodes"][0]})[0], 401, "one-use recovery")
    check(client.call("totp/disable", {"password": password, "code": enabled["recoveryCodes"][1]})[0], 204, "disable TOTP")
    assert not client.cookies
    print("TOTP setup, activation, recovery login and deactivation: passed", flush=True)

    check(client.call("login", {"email": email, "password": password})[0], 200, "login before reset")
    before = set(outbox.glob("*.txt"))
    check(client.call("forgot-password", {"email": email})[0], 202, "request reset")
    reset = email_token(outbox, "Reset your Zelo password", before)
    new_password = secrets.token_urlsafe(18)
    check(client.call("reset-password", {"token": reset, "password": new_password})[0], 204, "reset password")
    assert not client.cookies
    check(client.call("reset-password", {"token": reset, "password": new_password})[0], 400, "one-use reset")
    check(client.call("login", {"email": email, "password": password})[0], 401, "old password invalid")
    check(client.call("login", {"email": email, "password": new_password})[0], 200, "new password")
    check(client.call("forgot-password", {"email": "unknown@example.test"})[0], 202, "non-enumerating reset")
    check(client.call("login", {}, origin="https://other.example")[0], 403, "cross-origin rejection")
    check(client.call("login", method="GET")[0], 405, "wrong method")
    print("Password reset, token replay and origin protections: passed", flush=True)

    before = set(outbox.glob("*.txt"))
    user_email = f"web-user-{secrets.token_hex(6)}@example.test"
    status, result = client.call("register-user", {"name": "Invited User", "email": user_email, "password": password})
    check(status, 201, "user-only registration")
    assert result["confirmationEmailSent"]
    token = email_token(outbox, "Confirm your Zelo email", before)
    check(client.call("confirm-email", {"token": token})[0], 204, "user-only confirmation")
    check(client.call("login", {"email": user_email, "password": password})[0], 200, "user-only login")
    status, session = client.call("session")
    check(status, 200, "user-only session")
    assert session["accounts"] == []
    print("User-only registration and session: passed", flush=True)


def wait_ready(process, url, log):
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        if process.poll() is not None:
            log.seek(0)
            raise RuntimeError(log.read().decode(errors="replace")[-4000:])
        try:
            urllib.request.urlopen(url, timeout=2)
            return
        except urllib.error.HTTPError:
            return
        except (urllib.error.URLError, TimeoutError):
            time.sleep(0.2)
    raise RuntimeError("Test server did not start")


def main():
    if not (WEB / ".next/BUILD_ID").exists():
        raise RuntimeError("Run npm run build before this test")
    subprocess.run(["dotnet", "build", "Api/Zelo/Zelo.sln"], cwd=ROOT, check=True, stdout=subprocess.DEVNULL)
    database = "zelo_web_smoke_" + secrets.token_hex(5)
    subprocess.run(["createdb", database], check=True)
    processes = []
    try:
        for migration in sorted((ROOT / "Api/Zelo.Infrastructure/Data/Migrations").glob("*.sql")):
            subprocess.run(["psql", "-v", "ON_ERROR_STOP=1", "-d", database, "-f", str(migration)], check=True, stdout=subprocess.DEVNULL)
        with tempfile.TemporaryDirectory(prefix="zelo-web-smoke-") as directory, tempfile.TemporaryFile() as api_log, tempfile.TemporaryFile() as web_log:
            api_url = f"http://127.0.0.1:{free_port()}"
            web_port = free_port()
            web_url = f"http://127.0.0.1:{web_port}"
            env = os.environ.copy()
            connection = f"Host={env.get('PGHOST', 'localhost')};Port={env.get('PGPORT', '5432')};Database={database};Username={env.get('PGUSER', getpass.getuser())}"
            if env.get("PGPASSWORD"):
                connection += f";Password={env['PGPASSWORD']}"
            env.update({"ASPNETCORE_ENVIRONMENT": "Development", "ConnectionStrings__Zelo": connection, "Jwt__SigningKey": secrets.token_urlsafe(48), "Auth__TotpEncryptionKey": base64.b64encode(os.urandom(32)).decode(), "Auth__DevelopmentOutbox": directory, "Auth__RateLimitPerMinute": "200"})
            api = subprocess.Popen(["dotnet", "run", "--no-build", "--no-launch-profile", "--project", "Api/Zelo/Zelo.Web.Api.csproj", "--urls", api_url], cwd=ROOT, env=env, stdout=api_log, stderr=subprocess.STDOUT)
            processes.append(api)
            wait_ready(api, api_url + "/api/auth/me", api_log)
            web_env = os.environ.copy()
            web_env.update({"API_BASE_URL": api_url, "NODE_ENV": "production"})
            web = subprocess.Popen(["node", "node_modules/next/dist/bin/next", "start", "--hostname", "127.0.0.1", "--port", str(web_port)], cwd=WEB, env=web_env, stdout=web_log, stderr=subprocess.STDOUT)
            processes.append(web)
            wait_ready(web, web_url + "/login", web_log)
            exercise(BrowserClient(web_url), Path(directory))
    finally:
        for process in reversed(processes):
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
        subprocess.run(["dropdb", database], check=True)


if __name__ == "__main__":
    main()
