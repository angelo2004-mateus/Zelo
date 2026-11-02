BEGIN;

CREATE TABLE invitations (
    id uuid PRIMARY KEY,
    tenantid uuid NOT NULL REFERENCES accounts (id),
    email text NOT NULL CHECK (length(btrim(email)) > 0),
    roleid uuid NOT NULL,
    invitedbyuserid uuid NOT NULL REFERENCES users (id),
    status integer NOT NULL CHECK (status IN (0, 1, 2, 3)),
    tokenhash text NOT NULL UNIQUE,
    expirationtime timestamptz NOT NULL,
    acceptedtime timestamptz,
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz,
    CONSTRAINT invitationsrolefk FOREIGN KEY (tenantid, roleid) REFERENCES roles (tenantid, id)
);

CREATE UNIQUE INDEX invitationspendingemailciuq
    ON invitations (tenantid, lower(email)) WHERE status = 0;
CREATE INDEX invitationstenantididx ON invitations (tenantid);

INSERT INTO schemamigrations (version, appliedtime) VALUES (4, now());
COMMIT;
