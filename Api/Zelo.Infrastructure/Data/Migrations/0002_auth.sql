BEGIN;

CREATE TABLE userpasswordcredentials (
    id uuid PRIMARY KEY,
    userid uuid NOT NULL UNIQUE REFERENCES users (id),
    passwordhash text NOT NULL CHECK (length(btrim(passwordhash)) > 0),
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz
);

INSERT INTO schemamigrations (version, appliedtime) VALUES (2, now());

COMMIT;
