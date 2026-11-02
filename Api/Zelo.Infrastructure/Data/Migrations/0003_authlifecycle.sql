BEGIN;

ALTER TABLE users ADD COLUMN emailconfirmationtime timestamptz;
UPDATE users SET emailconfirmationtime = creationtime;
ALTER TABLE userpasswordcredentials ADD COLUMN failedlogincount integer NOT NULL DEFAULT 0;
ALTER TABLE userpasswordcredentials ADD COLUMN lockoutendtime timestamptz;

CREATE TABLE userauthtokens (
    id uuid PRIMARY KEY,
    userid uuid NOT NULL REFERENCES users (id),
    tokenhash text NOT NULL UNIQUE,
    purpose integer NOT NULL CHECK (purpose IN (0, 1, 2, 3)),
    expirationtime timestamptz NOT NULL,
    consumptiontime timestamptz,
    failedattemptcount integer NOT NULL DEFAULT 0,
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz
);
CREATE INDEX userauthtokensuseridpurposeidx ON userauthtokens (userid, purpose);

CREATE TABLE userauthsessions (
    id uuid PRIMARY KEY,
    userid uuid NOT NULL REFERENCES users (id),
    refreshtokenhash text NOT NULL UNIQUE,
    expirationtime timestamptz NOT NULL,
    revocationtime timestamptz,
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz
);
CREATE INDEX userauthsessionsuserididx ON userauthsessions (userid);

CREATE TABLE usertotpcredentials (
    id uuid PRIMARY KEY,
    userid uuid NOT NULL UNIQUE REFERENCES users (id),
    protectedsecret text NOT NULL,
    enabledtime timestamptz,
    lastusedstep bigint,
    failedattemptcount integer NOT NULL DEFAULT 0,
    lockoutendtime timestamptz,
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz
);

INSERT INTO schemamigrations (version, appliedtime) VALUES (3, now());
COMMIT;
