"""Start a loopback-only Mailpit inbox for local development."""

import os
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LOCAL = ROOT / "web/.local"


def main():
    executable = shutil.which("mailpit")
    if executable is None:
        raise SystemExit("Mailpit is required. Install it with: brew install mailpit")

    LOCAL.mkdir(exist_ok=True, mode=0o700)
    print("Virtual inbox: http://127.0.0.1:8025", flush=True)
    print("SMTP receiver: 127.0.0.1:1025", flush=True)
    os.execv(
        executable,
        [
            executable,
            "--listen", "127.0.0.1:8025",
            "--smtp", "127.0.0.1:1025",
            "--database", str(LOCAL / "mailpit.db"),
            "--allowed-hosts", "127.0.0.1,localhost",
        ],
    )


if __name__ == "__main__":
    main()
