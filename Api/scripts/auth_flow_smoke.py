"""Run the full local authentication flow against a disposable PostgreSQL database.

Requirements: dotnet, PostgreSQL client tools, and a locally reachable PostgreSQL server.
The same PGHOST, PGPORT, PGUSER, and PGPASSWORD variables used by psql are honored.
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
import urllib.request
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
MIGRATIONS = ROOT / "Api/Zelo.Infrastructure/Data/Migrations"


def request(base_url, path, payload=None, access_token=None, method=None):
    headers = {"Content-Type": "application/json"}
    if access_token is not None:
        headers["Authorization"] = "Bearer " + access_token

    data = json.dumps(payload).encode() if payload is not None else None
    call = urllib.request.Request(
        base_url + (path if path.startswith("/") else "/api/auth/" + path),
        data=data,
        headers=headers,
        method=method or ("POST" if payload is not None else "GET"),
    )
    try:
        with urllib.request.urlopen(call, timeout=10) as response:
            body = response.read()
            return response.status, json.loads(body) if body else None
    except urllib.error.HTTPError as error:
        return error.code, None


def expect(actual, expected, label):
    if actual != expected:
        raise AssertionError(f"{label}: expected HTTP {expected}, got {actual}")


def emailed_token(outbox, subject, previous_files):
    deadline = time.monotonic() + 10
    while time.monotonic() < deadline:
        new_files = set(outbox.glob("*.txt")) - previous_files
        for path in new_files:
            content = path.read_text(encoding="utf-8-sig")
            if f"Subject: {subject}" in content:
                match = re.search(r"token: ([A-Za-z0-9_-]+)", content)
                if match is not None:
                    return match.group(1)
        time.sleep(0.1)
    raise AssertionError(f"No local email received for {subject}")


def totp(secret, step=None):
    if step is None:
        step = int(time.time()) // 30
    key = base64.b32decode(secret)
    digest = hmac.new(key, struct.pack(">Q", step), hashlib.sha1).digest()
    offset = digest[-1] & 15
    value = struct.unpack(">I", digest[offset : offset + 4])[0] & 0x7FFFFFFF
    return f"{value % 1_000_000:06d}"


def wait_for_next_totp_step():
    time.sleep(31 - int(time.time()) % 30)


def update_database(database, statement):
    subprocess.run(
        ["psql", "-v", "ON_ERROR_STOP=1", "-d", database, "-c", statement],
        cwd=ROOT,
        check=True,
        stdout=subprocess.DEVNULL,
    )


def query_database(database, statement):
    result = subprocess.run(
        ["psql", "-v", "ON_ERROR_STOP=1", "-At", "-d", database, "-c", statement],
        cwd=ROOT,
        check=True,
        capture_output=True,
        text=True,
    )
    return result.stdout.strip()


def exercise(base_url, outbox, database):
    failed_email = f"rollback-{secrets.token_hex(6)}@example.test"
    update_database(
        database,
        "CREATE FUNCTION reject_smoke_role() RETURNS trigger LANGUAGE plpgsql "
        "AS $$ BEGIN RAISE EXCEPTION 'Forced role setup failure'; END; $$; "
        "CREATE TRIGGER reject_smoke_role BEFORE INSERT ON roles "
        "FOR EACH ROW EXECUTE FUNCTION reject_smoke_role();",
    )
    try:
        status, _ = request(
            base_url,
            "register",
            {
                "email": failed_email,
                "password": "Smoke-Rollback-Password-1234",
                "name": "Rollback Smoke",
                "accountName": "Rollback Smoke Account",
            },
        )
        expect(status, 500, "role setup failure")
    finally:
        update_database(
            database,
            "DROP TRIGGER reject_smoke_role ON roles; "
            "DROP FUNCTION reject_smoke_role();",
        )
    assert query_database(
        database, f"SELECT count(*) FROM users WHERE email = '{failed_email}'"
    ) == "0"
    assert query_database(
        database,
        "SELECT count(*) FROM accounts WHERE name = 'Rollback Smoke Account'",
    ) == "0"
    print("role setup failure rolls back user and account: passed")

    email = f"auth-smoke-{secrets.token_hex(6)}@example.test"
    initial_password = "Smoke-Initial-Password-1234"
    new_password = "Smoke-New-Password-5678"
    registration = {
        "email": email,
        "password": initial_password,
        "name": "Auth Smoke",
        "accountName": "Auth Smoke Account",
    }

    before = set(outbox.glob("*.txt"))
    status, registration_result = request(base_url, "register", registration)
    expect(status, 201, "registration")
    assert registration_result["confirmationEmailSent"] is True
    tenant_id = registration_result["account"]["id"]
    status, _ = request(base_url, "register", registration)
    expect(status, 409, "duplicate registration")
    status, _ = request(base_url, "login", {"email": email, "password": initial_password})
    expect(status, 401, "unconfirmed login")

    confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    before = set(outbox.glob("*.txt"))
    status, _ = request(base_url, "resend-confirmation", {"email": email})
    expect(status, 202, "resend confirmation")
    replacement_confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    status, _ = request(base_url, "confirm-email", {"token": confirmation})
    expect(status, 400, "old confirmation invalidated by resend")
    status, _ = request(base_url, "confirm-email", {"token": replacement_confirmation})
    expect(status, 204, "email confirmation")
    status, _ = request(base_url, "confirm-email", {"token": replacement_confirmation})
    expect(status, 400, "replayed confirmation")
    print("registration and email confirmation: passed")

    status, login = request(base_url, "login", {"email": email, "password": initial_password})
    expect(status, 200, "login")
    assert login["accessToken"] and login["refreshToken"]
    assert login["refreshExpirationTime"] and len(login["accounts"]) == 1
    status, _ = request(base_url, "me", access_token=login["accessToken"])
    expect(status, 200, "authenticated request")

    status, refreshed = request(
        base_url, "refresh", {"refreshToken": login["refreshToken"]}
    )
    expect(status, 200, "refresh")
    assert refreshed["refreshExpirationTime"]
    status, _ = request(base_url, "refresh", {"refreshToken": login["refreshToken"]})
    expect(status, 401, "replayed refresh")
    status, _ = request(base_url, "me", access_token=login["accessToken"])
    expect(status, 401, "old access token after refresh")
    status, _ = request(base_url, "logout", {}, refreshed["accessToken"])
    expect(status, 204, "logout")
    status, _ = request(base_url, "me", access_token=refreshed["accessToken"])
    expect(status, 401, "revoked access token")
    print("login, refresh rotation, and logout: passed")

    status, current = request(base_url, "login", {"email": email, "password": initial_password})
    expect(status, 200, "login before reset")
    status, _ = request(
        base_url, "forgot-password", {"email": "unknown@example.test"}
    )
    expect(status, 202, "unknown email reset request")
    before = set(outbox.glob("*.txt"))
    status, _ = request(base_url, "forgot-password", {"email": email})
    expect(status, 202, "password reset request")
    old_reset_token = emailed_token(outbox, "Reset your Zelo password", before)
    before = set(outbox.glob("*.txt"))
    status, _ = request(base_url, "forgot-password", {"email": email})
    expect(status, 202, "second password reset request")
    reset_token = emailed_token(outbox, "Reset your Zelo password", before)
    status, _ = request(
        base_url, "reset-password", {"token": old_reset_token, "password": new_password}
    )
    expect(status, 400, "old reset token invalidated")
    status, _ = request(
        base_url, "reset-password", {"token": reset_token, "password": new_password}
    )
    expect(status, 204, "password reset")
    status, _ = request(
        base_url, "reset-password", {"token": reset_token, "password": new_password}
    )
    expect(status, 400, "replayed password reset")
    status, _ = request(base_url, "me", access_token=current["accessToken"])
    expect(status, 401, "session revoked by password reset")
    status, _ = request(base_url, "login", {"email": email, "password": initial_password})
    expect(status, 401, "old password")
    print("password reset and session invalidation: passed")

    for _ in range(5):
        status, _ = request(base_url, "login", {"email": email, "password": "wrong-password"})
        expect(status, 401, "failed password")
    status, _ = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 401, "password lockout")
    print("durable password lockout: passed")

    before = set(outbox.glob("*.txt"))
    status, _ = request(base_url, "forgot-password", {"email": email})
    expect(status, 202, "reset during lockout")
    reset_token = emailed_token(outbox, "Reset your Zelo password", before)
    status, _ = request(
        base_url, "reset-password", {"token": reset_token, "password": new_password}
    )
    expect(status, 204, "reset clears lockout")
    status, current = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 200, "login after reset")

    owner_access = current["accessToken"]
    account_path = f"/api/accounts/{tenant_id}"
    status, company = request(
        base_url, account_path + "/companies",
        {
            "legalName": "First Company Ltda",
            "tradeName": "First Company",
            "federalTaxNumber": "12.ABC.678/0001-90",
        },
        owner_access,
    )
    expect(status, 201, "owner creates company")
    assert company["tenantId"] == tenant_id and company["federalTaxNumber"] == "12ABC678000190"
    assert query_database(
        database,
        f"SELECT creatorid = '{registration_result['user']['id']}'::uuid "
        f"AND creationtime > now() - interval '1 minute' "
        f"FROM companies WHERE id = '{company['id']}'::uuid",
    ) == "t"
    status, roles = request(base_url, account_path + "/roles", access_token=owner_access)
    expect(status, 200, "owner role list")
    role_ids = {role["name"]: role["id"] for role in roles}
    assert {"Owner", "Administrator", "Accountant", "ReadOnly"} <= set(role_ids)
    status, custom_role = request(
        base_url, account_path + "/roles",
        {"name": "FinanceReviewer", "permissions": ["finance.read"]},
        owner_access,
    )
    expect(status, 201, "custom role creation")
    status, _ = request(
        base_url, account_path + f"/roles/{custom_role['id']}",
        {"name": "CompanyOperator", "permissions": ["finance.read", "company.manage"]},
        owner_access,
        method="PUT",
    )
    expect(status, 200, "custom role update")
    status, _ = request(
        base_url, account_path + f"/roles/{role_ids['Owner']}",
        {"name": "RenamedOwner", "permissions": ["finance.read"]},
        owner_access,
        method="PUT",
    )
    expect(status, 403, "Owner role is immutable")
    status, privileged_role = request(
        base_url, account_path + "/roles",
        {"name": "AccountManager", "permissions": ["account.manage"]},
        owner_access,
    )
    expect(status, 201, "privileged custom role")
    status, _ = request(
        base_url, account_path + "/roles",
        {"name": "InvalidRole", "permissions": ["finance.unknown"]},
        owner_access,
    )
    expect(status, 400, "unknown permission rejected")

    invited_email = f"invited-{secrets.token_hex(6)}@example.test"
    invited_password = "Smoke-Invited-Password-1234"
    before = set(outbox.glob("*.txt"))
    status, invitation = request(
        base_url, account_path + "/invitations",
        {"email": invited_email, "roleId": role_ids["Accountant"]},
        owner_access,
    )
    expect(status, 201, "invite accountant")
    assert invitation["emailSent"] is True
    invite_token = emailed_token(outbox, "You are invited to a Zelo account", before)
    status, _ = request(base_url, "/api/invitations/accept", {"token": invite_token}, owner_access)
    expect(status, 403, "wrong email cannot accept invitation")

    before = set(outbox.glob("*.txt"))
    status, invited_registration = request(
        base_url,
        "register-user",
        {"email": invited_email, "password": invited_password, "name": "Invited User"},
    )
    expect(status, 201, "register invited user without account")
    invited_id = invited_registration["user"]["id"]
    confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    status, _ = request(base_url, "confirm-email", {"token": confirmation})
    expect(status, 204, "invited user confirmation")
    status, invited_auth = request(
        base_url, "login", {"email": invited_email, "password": invited_password}
    )
    expect(status, 200, "invited user login")
    assert invited_auth["accounts"] == []
    invited_access = invited_auth["accessToken"]
    status, _ = request(base_url, "/api/invitations/accept", {"token": invite_token}, invited_access)
    expect(status, 200, "invitation acceptance")
    status, _ = request(base_url, "/api/invitations/accept", {"token": invite_token}, invited_access)
    expect(status, 409, "replayed invitation")
    status, account_list = request(base_url, "/api/accounts", access_token=invited_access)
    expect(status, 200, "invited account access")
    assert len(account_list) == 1 and account_list[0]["roleName"] == "Accountant"
    status, _ = request(base_url, account_path + "/roles", access_token=invited_access)
    expect(status, 403, "accountant cannot manage roles")
    status, companies = request(
        base_url, account_path + "/companies", access_token=invited_access
    )
    expect(status, 200, "accountant views all tenant companies")
    assert len(companies) == 1
    status, _ = request(
        base_url, account_path + "/companies",
        {"legalName": "Denied Company", "federalTaxNumber": "12345678000190"},
        invited_access,
    )
    expect(status, 403, "accountant cannot create company")

    status, _ = request(
        base_url, account_path + f"/members/{invited_id}/role",
        {"roleId": custom_role["id"]}, owner_access, method="PATCH",
    )
    expect(status, 204, "assign custom role")
    assert query_database(
        database,
        f"SELECT lastmodifierid = '{registration_result['user']['id']}'::uuid "
        f"AND lastmodifiedtime > now() - interval '1 minute' "
        f"FROM usertenants WHERE tenantid = '{tenant_id}'::uuid AND userid = '{invited_id}'::uuid",
    ) == "t"
    status, _ = request(
        base_url, account_path + "/companies",
        {"legalName": "Role Granted Company", "federalTaxNumber": "12345678000490"},
        invited_access,
    )
    expect(status, 201, "custom role grants company.manage")
    status, _ = request(
        base_url, account_path + f"/roles/{custom_role['id']}",
        {"name": "CompanyOperator", "permissions": ["finance.read"]},
        owner_access,
        method="PUT",
    )
    expect(status, 200, "remove custom role permission")
    status, _ = request(
        base_url, account_path + "/companies",
        {"legalName": "Denied Again", "federalTaxNumber": "12345678000590"},
        invited_access,
    )
    expect(status, 403, "role permission removal takes effect immediately")
    print("invitation, account membership, and permission denial: passed")

    status, _ = request(
        base_url,
        account_path + f"/members/{invited_id}/role",
        {"roleId": role_ids["Administrator"]},
        owner_access,
        method="PATCH",
    )
    expect(status, 204, "promote invited member")
    status, _ = request(base_url, account_path + "/roles", access_token=invited_access)
    expect(status, 200, "new permission effective immediately")
    status, _ = request(
        base_url, account_path + "/companies",
        {"legalName": "Second Company Ltda", "federalTaxNumber": "12345678000290"},
        invited_access,
    )
    expect(status, 201, "administrator can create company")
    status, _ = request(
        base_url, account_path + "/invitations",
        {"email": f"owner-attempt-{secrets.token_hex(4)}@example.test", "roleId": role_ids["Owner"]},
        invited_access,
    )
    expect(status, 403, "non-owner cannot grant Owner")
    status, _ = request(
        base_url, account_path + "/roles",
        {"name": "EscalatedRole", "permissions": ["account.manage"]},
        invited_access,
    )
    expect(status, 403, "administrator cannot grant permission they lack")
    status, _ = request(
        base_url, account_path + f"/members/{invited_id}/role",
        {"roleId": privileged_role["id"]},
        invited_access,
        method="PATCH",
    )
    expect(status, 403, "administrator cannot assign stronger role")

    stale_inviter_email = f"stale-inviter-{secrets.token_hex(4)}@example.test"
    before = set(outbox.glob("*.txt"))
    status, _ = request(
        base_url, account_path + "/invitations",
        {"email": stale_inviter_email, "roleId": role_ids["ReadOnly"]},
        invited_access,
    )
    expect(status, 201, "administrator invites member")
    stale_invite_token = emailed_token(
        outbox, "You are invited to a Zelo account", before
    )
    status, _ = request(
        base_url, account_path + f"/members/{invited_id}/role",
        {"roleId": role_ids["Accountant"]}, owner_access, method="PATCH",
    )
    expect(status, 204, "inviter loses member.manage")
    before = set(outbox.glob("*.txt"))
    status, _ = request(
        base_url,
        "register-user",
        {
            "email": stale_inviter_email,
            "password": "Stale-Inviter-Password-1234",
            "name": "Stale Inviter Recipient",
        },
    )
    expect(status, 201, "register pending recipient")
    confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    status, _ = request(base_url, "confirm-email", {"token": confirmation})
    expect(status, 204, "pending recipient confirms")
    status, stale_recipient = request(
        base_url,
        "login",
        {"email": stale_inviter_email, "password": "Stale-Inviter-Password-1234"},
    )
    expect(status, 200, "pending recipient login")
    status, _ = request(
        base_url, "/api/invitations/accept",
        {"token": stale_invite_token}, stale_recipient["accessToken"],
    )
    expect(status, 403, "acceptance rechecks inviter permission")
    status, _ = request(
        base_url, account_path + f"/members/{invited_id}/role",
        {"roleId": role_ids["Administrator"]}, owner_access, method="PATCH",
    )
    expect(status, 204, "restore administrator role")

    status, _ = request(
        base_url, account_path + f"/members/{invited_id}/status",
        {"status": 1}, owner_access, method="PATCH",
    )
    expect(status, 204, "suspend member")
    status, _ = request(base_url, account_path, access_token=invited_access)
    expect(status, 404, "suspended member loses account")
    status, _ = request(
        base_url, account_path + "/invitations",
        {"email": invited_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 409, "suspended member cannot be silently reinvited")
    status, _ = request(
        base_url, account_path + f"/members/{invited_id}/status",
        {"status": 0}, owner_access, method="PATCH",
    )
    expect(status, 204, "reactivate suspended member")
    status, _ = request(base_url, account_path + "/roles", access_token=invited_access)
    expect(status, 200, "restored permissions effective immediately")
    status, members = request(base_url, account_path + "/members", access_token=owner_access)
    expect(status, 200, "owner member list")
    assert len(members) == 2
    status, _ = request(
        base_url, account_path + f"/members/{registration_result['user']['id']}",
        {}, owner_access, method="DELETE",
    )
    expect(status, 409, "last Owner cannot be removed")
    status, _ = request(
        base_url,
        account_path + f"/members/{registration_result['user']['id']}/status",
        {"status": 1}, owner_access, method="PATCH",
    )
    expect(status, 409, "last Owner cannot be suspended")

    pending_email = f"pending-{secrets.token_hex(4)}@example.test"
    before = set(outbox.glob("*.txt"))
    status, pending = request(
        base_url, account_path + "/invitations",
        {"email": pending_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 201, "pending invitation")
    first_invite_token = emailed_token(outbox, "You are invited to a Zelo account", before)
    before = set(outbox.glob("*.txt"))
    status, resent = request(
        base_url, account_path + "/invitations",
        {"email": pending_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 201, "resend invitation")
    assert resent["id"] == pending["id"]
    second_invite_token = emailed_token(outbox, "You are invited to a Zelo account", before)
    status, _ = request(
        base_url, "/api/invitations/accept", {"token": first_invite_token}, owner_access
    )
    expect(status, 404, "resend invalidates old token")
    status, _ = request(
        base_url,
        account_path + f"/invitations/{pending['id']}/revoke",
        {}, owner_access,
    )
    expect(status, 204, "revoke invitation")
    status, _ = request(
        base_url, "/api/invitations/accept", {"token": second_invite_token}, owner_access
    )
    expect(status, 409, "revoked invitation unusable")

    expired_email = f"expired-{secrets.token_hex(4)}@example.test"
    before = set(outbox.glob("*.txt"))
    status, expired_invite = request(
        base_url, account_path + "/invitations",
        {"email": expired_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 201, "invitation to expire")
    expired_token = emailed_token(outbox, "You are invited to a Zelo account", before)
    update_database(
        database,
        f"UPDATE invitations SET expirationtime = now() - interval '1 minute' "
        f"WHERE id = '{expired_invite['id']}'",
    )
    before = set(outbox.glob("*.txt"))
    status, replacement_invite = request(
        base_url, account_path + "/invitations",
        {"email": expired_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 201, "replace expired pending invitation")
    assert replacement_invite["id"] != expired_invite["id"]
    emailed_token(outbox, "You are invited to a Zelo account", before)
    status, _ = request(
        base_url, "/api/invitations/accept", {"token": expired_token}, owner_access
    )
    expect(status, 409, "expired invitation unusable")

    status, _ = request(
        base_url, account_path + f"/members/{invited_id}",
        {}, owner_access, method="DELETE",
    )
    expect(status, 204, "revoke membership")
    status, _ = request(base_url, account_path, access_token=invited_access)
    expect(status, 404, "revoked membership loses account")
    before = set(outbox.glob("*.txt"))
    status, _ = request(
        base_url, account_path + "/invitations",
        {"email": invited_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 201, "reinvite revoked member")
    invite_token = emailed_token(outbox, "You are invited to a Zelo account", before)
    status, _ = request(base_url, "/api/invitations/accept", {"token": invite_token}, invited_access)
    expect(status, 200, "reactivate revoked membership")
    status, _ = request(base_url, account_path + "/members", access_token=invited_access)
    expect(status, 403, "read-only member cannot list members")
    status, companies = request(
        base_url, account_path + "/companies", access_token=invited_access
    )
    expect(status, 200, "read-only member sees every tenant company")
    assert len(companies) == 3
    print("role change, last Owner, revocation, and reinvitation: passed")

    second_email = f"second-owner-{secrets.token_hex(6)}@example.test"
    before = set(outbox.glob("*.txt"))
    status, second_registration = request(
        base_url,
        "register",
        {
            "email": second_email,
            "password": "Second-Owner-Password-1234",
            "name": "Second Owner",
            "accountName": "Second Account",
        },
    )
    expect(status, 201, "second account registration")
    second_tenant = second_registration["account"]["id"]
    confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    status, _ = request(base_url, "confirm-email", {"token": confirmation})
    expect(status, 204, "second owner confirmation")
    status, second_auth = request(
        base_url,
        "login",
        {"email": second_email, "password": "Second-Owner-Password-1234"},
    )
    expect(status, 200, "second owner login")
    second_path = f"/api/accounts/{second_tenant}"
    status, second_company = request(
        base_url,
        second_path + "/companies",
        {"legalName": "Second Tenant Company", "federalTaxNumber": "12345678000690"},
        second_auth["accessToken"],
    )
    expect(status, 201, "second tenant company creation")
    status, second_roles = request(
        base_url, second_path + "/roles", access_token=second_auth["accessToken"]
    )
    expect(status, 200, "second account roles")
    second_accountant = next(
        role["id"] for role in second_roles if role["name"] == "Accountant"
    )
    before = set(outbox.glob("*.txt"))
    status, _ = request(
        base_url, second_path + "/invitations",
        {"email": invited_email, "roleId": second_accountant},
        second_auth["accessToken"],
    )
    expect(status, 201, "second account invitation")
    invite_token = emailed_token(outbox, "You are invited to a Zelo account", before)
    status, _ = request(base_url, "/api/invitations/accept", {"token": invite_token}, invited_access)
    expect(status, 200, "same user joins second account")
    status, account_list = request(base_url, "/api/accounts", access_token=invited_access)
    expect(status, 200, "two accounts for one user")
    assert {account["accountId"] for account in account_list} == {tenant_id, second_tenant}
    assert {account["roleName"] for account in account_list} == {"ReadOnly", "Accountant"}
    status, first_companies = request(
        base_url, account_path + "/companies", access_token=invited_access
    )
    expect(status, 200, "first tenant company query")
    assert len(first_companies) == 3
    status, second_companies = request(
        base_url, second_path + "/companies", access_token=invited_access
    )
    expect(status, 200, "second tenant company query")
    assert [item["id"] for item in second_companies] == [second_company["id"]]
    status, _ = request(base_url, second_path, access_token=owner_access)
    expect(status, 404, "first owner cannot access second account")
    status, _ = request(base_url, second_path + "/companies", access_token=owner_access)
    expect(status, 403, "first owner cannot read second account companies")
    print("one user in two accounts with different roles: passed")

    admin_email = f"platform-admin-{secrets.token_hex(6)}@example.test"
    before = set(outbox.glob("*.txt"))
    status, _ = request(
        base_url, "register-user",
        {
            "email": admin_email,
            "password": "Platform-Admin-Password-1234",
            "name": "Platform Admin",
        },
    )
    expect(status, 201, "create platform admin identity")
    confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    status, _ = request(base_url, "confirm-email", {"token": confirmation})
    expect(status, 204, "platform admin confirmation")
    update_database(database, f"UPDATE users SET userrole = 1 WHERE email = '{admin_email}'")
    status, platform_auth = request(
        base_url,
        "login",
        {"email": admin_email, "password": "Platform-Admin-Password-1234"},
    )
    expect(status, 200, "platform admin login")
    platform_access = platform_auth["accessToken"]
    status, accounts = request(base_url, "/api/accounts", access_token=platform_access)
    expect(status, 200, "platform admin account list")
    assert {account["accountId"] for account in accounts} == {tenant_id, second_tenant}
    status, _ = request(base_url, second_path + "/roles", access_token=platform_access)
    expect(status, 200, "platform admin manages account without membership")
    status, _ = request(
        base_url, second_path + "/companies",
        {"legalName": "Admin Created Ltda", "federalTaxNumber": "12345678000390"},
        platform_access,
    )
    expect(status, 201, "platform admin creates company without membership")
    update_database(database, f"UPDATE accounts SET status = 1 WHERE id = '{second_tenant}'")
    status, accounts = request(base_url, "/api/accounts", access_token=platform_access)
    expect(status, 200, "platform admin sees suspended account")
    assert next(account for account in accounts if account["accountId"] == second_tenant)["isActive"] is False
    status, accounts = request(base_url, "/api/accounts", access_token=invited_access)
    expect(status, 200, "regular member account list during suspension")
    assert len(accounts) == 1 and accounts[0]["accountId"] == tenant_id
    update_database(database, f"UPDATE accounts SET status = 0 WHERE id = '{second_tenant}'")
    update_database(database, f"UPDATE users SET status = 1 WHERE email = '{admin_email}'")
    status, _ = request(base_url, "me", access_token=platform_access)
    expect(status, 401, "disabled platform admin loses access")
    print("platform admin access, suspended tenant, and user disablement: passed")

    hidden_outbox = outbox.with_name(outbox.name + "-unavailable")
    outbox.rename(hidden_outbox)
    delivery_failure_email = f"delivery-failure-{secrets.token_hex(4)}@example.test"
    try:
        status, _ = request(base_url, "forgot-password", {"email": email})
        expect(status, 202, "email failure does not reveal account existence")
        status, failed_registration = request(
            base_url,
            "register-user",
            {
                "email": delivery_failure_email,
                "password": "Delivery-Failure-Password-1234",
                "name": "Delivery Failure",
            },
        )
        expect(status, 201, "registration survives email failure")
        assert failed_registration["confirmationEmailSent"] is False
        status, failed_invitation = request(
            base_url, account_path + "/invitations",
            {
                "email": delivery_failure_email,
                "roleId": role_ids["ReadOnly"],
            },
            owner_access,
        )
        expect(status, 201, "invitation survives email failure")
        assert failed_invitation["emailSent"] is False
    finally:
        hidden_outbox.rename(outbox)

    before = set(outbox.glob("*.txt"))
    status, _ = request(
        base_url, "resend-confirmation", {"email": delivery_failure_email}
    )
    expect(status, 202, "resend after email failure")
    confirmation = emailed_token(outbox, "Confirm your Zelo email", before)
    status, _ = request(base_url, "confirm-email", {"token": confirmation})
    expect(status, 204, "confirmation after delivery recovered")
    before = set(outbox.glob("*.txt"))
    status, resent = request(
        base_url, account_path + "/invitations",
        {"email": delivery_failure_email, "roleId": role_ids["ReadOnly"]},
        owner_access,
    )
    expect(status, 201, "invitation resend after delivery recovered")
    assert resent["emailSent"] is True
    emailed_token(outbox, "You are invited to a Zelo account", before)
    print("email delivery failure and recovery: passed")

    status, setup = request(
        base_url, "totp/setup", {"password": new_password}, current["accessToken"]
    )
    expect(status, 200, "TOTP setup")
    assert setup["secret"] in setup["authenticatorUri"]
    status, enabled = request(
        base_url, "totp/enable", {"code": totp(setup["secret"])}, current["accessToken"]
    )
    expect(status, 200, "TOTP enable")
    assert len(enabled["recoveryCodes"]) == 8
    status, _ = request(base_url, "me", access_token=current["accessToken"])
    expect(status, 401, "old session after TOTP enable")

    status, challenge = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 200, "TOTP login challenge")
    assert challenge["requiresTwoFactor"] and challenge["accessToken"] is None
    wait_for_next_totp_step()
    status, second_factor = request(
        base_url,
        "totp/verify",
        {"challengeToken": challenge["challengeToken"], "code": totp(setup["secret"])},
    )
    expect(status, 200, "TOTP verification")
    status, _ = request(
        base_url,
        "totp/verify",
        {"challengeToken": challenge["challengeToken"], "code": totp(setup["secret"])},
    )
    expect(status, 401, "replayed TOTP challenge")
    print("TOTP enrollment and login: passed")

    status, challenge = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 200, "recovery challenge")
    status, recovered = request(
        base_url,
        "totp/verify",
        {"challengeToken": challenge["challengeToken"], "code": enabled["recoveryCodes"][0]},
    )
    expect(status, 200, "recovery login")
    status, challenge = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 200, "reused recovery challenge")
    status, _ = request(
        base_url,
        "totp/verify",
        {"challengeToken": challenge["challengeToken"], "code": enabled["recoveryCodes"][0]},
    )
    expect(status, 401, "reused recovery code")
    status, _ = request(
        base_url,
        "totp/disable",
        {"password": new_password, "code": enabled["recoveryCodes"][1]},
        recovered["accessToken"],
    )
    expect(status, 204, "TOTP disable with recovery code")
    status, _ = request(base_url, "me", access_token=recovered["accessToken"])
    expect(status, 401, "session revoked by TOTP disable")
    print("one-use recovery codes and TOTP disable: passed")

    status, current = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 200, "login after TOTP disable")
    status, setup = request(
        base_url, "totp/setup", {"password": new_password}, current["accessToken"]
    )
    expect(status, 200, "second TOTP setup")
    status, _ = request(
        base_url, "totp/enable", {"code": totp(setup["secret"])}, current["accessToken"]
    )
    expect(status, 200, "second TOTP enable")
    status, challenge = request(base_url, "login", {"email": email, "password": new_password})
    expect(status, 200, "challenge for failure limit")
    for _ in range(5):
        status, _ = request(
            base_url,
            "totp/verify",
            {"challengeToken": challenge["challengeToken"], "code": "bad-code"},
        )
        expect(status, 401, "invalid TOTP code")
    status, _ = request(
        base_url,
        "totp/verify",
        {"challengeToken": challenge["challengeToken"], "code": totp(setup["secret"])},
    )
    expect(status, 401, "locked TOTP challenge")
    print("durable TOTP lockout and challenge attempt limit: passed")


def main():
    database = "zelo_auth_smoke_" + secrets.token_hex(5)
    subprocess.run(["dotnet", "build", "Api/Zelo/Zelo.sln"], cwd=ROOT, check=True)
    subprocess.run(["createdb", database], cwd=ROOT, check=True)
    server = None
    try:
        for name in (
            "0001_identity.sql", "0002_auth.sql",
            "0003_authlifecycle.sql", "0004_invitations.sql",
        ):
            subprocess.run(
                ["psql", "-v", "ON_ERROR_STOP=1", "-d", database, "-f", str(MIGRATIONS / name)],
                cwd=ROOT,
                check=True,
                stdout=subprocess.DEVNULL,
            )

        with tempfile.TemporaryDirectory(prefix="zelo-auth-outbox-") as directory:
            with socket.socket() as listener:
                listener.bind(("127.0.0.1", 0))
                port = listener.getsockname()[1]
            base_url = f"http://127.0.0.1:{port}"
            env = os.environ.copy()
            connection = (
                f"Host={env.get('PGHOST', 'localhost')};"
                f"Port={env.get('PGPORT', '5432')};"
                f"Database={database};"
                f"Username={env.get('PGUSER', getpass.getuser())}"
            )
            if env.get("PGPASSWORD"):
                connection += f";Password={env['PGPASSWORD']}"
            env.update(
                {
                    "ASPNETCORE_ENVIRONMENT": "Development",
                    "ConnectionStrings__Zelo": connection,
                    "Jwt__SigningKey": secrets.token_urlsafe(48),
                    "Auth__TotpEncryptionKey": base64.b64encode(os.urandom(32)).decode(),
                    "Auth__DevelopmentOutbox": directory,
                    "Auth__RateLimitPerMinute": "200",
                }
            )
            with tempfile.TemporaryFile() as server_log:
                server = subprocess.Popen(
                    [
                        "dotnet", "run", "--no-build", "--no-launch-profile",
                        "--project", "Api/Zelo/Zelo.Web.Api.csproj",
                        "--urls", base_url,
                    ],
                    cwd=ROOT,
                    env=env,
                    stdout=server_log,
                    stderr=subprocess.STDOUT,
                )
                deadline = time.monotonic() + 20
                while time.monotonic() < deadline:
                    if server.poll() is not None:
                        server_log.seek(0)
                        raise RuntimeError(server_log.read().decode())
                    try:
                        request(base_url, "me")
                        break
                    except urllib.error.URLError:
                        time.sleep(0.1)
                else:
                    raise RuntimeError("API did not start within 20 seconds")
                try:
                    exercise(base_url, Path(directory), database)
                except Exception:
                    server_log.seek(0)
                    print(server_log.read().decode(errors="replace")[-6000:])
                    raise
    finally:
        if server is not None:
            server.terminate()
            try:
                server.wait(timeout=10)
            except subprocess.TimeoutExpired:
                server.kill()
                server.wait()
        subprocess.run(["dropdb", database], cwd=ROOT, check=True)


if __name__ == "__main__":
    main()
