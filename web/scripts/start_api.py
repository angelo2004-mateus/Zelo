"""Start the real development API and prepare its dedicated local database."""

import getpass
import argparse
import json
import os
import secrets
import socket
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LOCAL = ROOT / "web/.local"
DATABASE = "zelo_dev"


def run(*arguments, capture=False):
    return subprocess.run(arguments, cwd=ROOT, check=True, text=True, stdout=subprocess.PIPE if capture else None)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--mailpit", action="store_true")
    arguments = parser.parse_args()
    if arguments.mailpit:
        try:
            with socket.create_connection(("127.0.0.1", 1025), timeout=1):
                pass
        except OSError as exception:
            raise SystemExit(
                "Mailpit is not running. Start it with: npm run dev:mail"
            ) from exception

    LOCAL.mkdir(exist_ok=True, mode=0o700)
    config_path = LOCAL / "api.json"
    if not config_path.exists():
        config_path.write_text(json.dumps({"signingKey": secrets.token_urlsafe(48)}))
        config_path.chmod(0o600)
    config = json.loads(config_path.read_text())
    existing = run("psql", "-d", "template1", "-Atc", "SELECT datname FROM pg_database WHERE datname = 'zelo_dev'", capture=True).stdout.strip()
    if not existing:
        run("createdb", DATABASE)
    has_migrations = run("psql", "-d", DATABASE, "-Atc", "SELECT to_regclass('public.schemamigrations') IS NOT NULL", capture=True).stdout.strip() == "t"
    applied = set()
    if has_migrations:
        applied = {int(value) for value in run("psql", "-d", DATABASE, "-Atc", "SELECT version FROM schemamigrations", capture=True).stdout.splitlines()}
    for migration in sorted((ROOT / "Api/Zelo.Infrastructure/Data/Migrations").glob("*.sql")):
        if int(migration.name.split('_')[0]) not in applied:
            run("psql", "-v", "ON_ERROR_STOP=1", "-d", DATABASE, "-f", str(migration))
    env = os.environ.copy()
    connection = f"Host={env.get('PGHOST', 'localhost')};Port={env.get('PGPORT', '5432')};Database={DATABASE};Username={env.get('PGUSER', getpass.getuser())}"
    if env.get("PGPASSWORD"):
        connection += f";Password={env['PGPASSWORD']}"
    env.update({"ASPNETCORE_ENVIRONMENT": "Development", "ConnectionStrings__Zelo": connection, "Jwt__SigningKey": config['signingKey'], "Auth__DevelopmentOutbox": str(LOCAL / 'mail')})
    if arguments.mailpit:
        env.update({
            "Auth__EmailDelivery": "Smtp",
            "Auth__Smtp__Host": "127.0.0.1",
            "Auth__Smtp__Port": "1025",
            "Auth__Smtp__From": "no-reply@zelo.test",
            "Auth__Smtp__Username": "",
            "Auth__Smtp__Password": "",
            "Auth__Smtp__Security": "None",
        })
    print("API: http://127.0.0.1:5271 | database: zelo_dev", flush=True)
    if arguments.mailpit:
        print("Virtual inbox: http://127.0.0.1:8025", flush=True)
    else:
        print(f"Development emails: {LOCAL / 'mail'}", flush=True)
    os.chdir(ROOT)
    os.execvpe("dotnet", ["dotnet", "run", "--no-launch-profile", "--project", "Api/Zelo/Zelo.Web.Api.csproj", "--urls", "http://127.0.0.1:5271"], env)


if __name__ == "__main__":
    main()
