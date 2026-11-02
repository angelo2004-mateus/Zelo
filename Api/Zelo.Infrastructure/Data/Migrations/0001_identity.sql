BEGIN;

CREATE TABLE schemamigrations (
    version integer PRIMARY KEY,
    appliedtime timestamptz NOT NULL
);

CREATE TABLE users (
    id uuid PRIMARY KEY,
    email text NOT NULL CHECK (length(btrim(email)) > 0),
    name text NOT NULL CHECK (length(btrim(name)) > 0),
    userrole integer NOT NULL CHECK (userrole IN (0, 1)),
    status integer NOT NULL CHECK (status IN (0, 1)),
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz
);

CREATE UNIQUE INDEX usersemailciuq ON users (lower(email));

CREATE TABLE accounts (
    id uuid PRIMARY KEY,
    name text NOT NULL CHECK (length(btrim(name)) > 0),
    status integer NOT NULL CHECK (status IN (0, 1)),
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz,
    creatorid uuid NOT NULL REFERENCES users (id),
    lastmodifierid uuid REFERENCES users (id),
    CONSTRAINT accountsmodificationauditck CHECK ((lastmodifiedtime IS NULL) = (lastmodifierid IS NULL))
);

CREATE TABLE roles (
    id uuid PRIMARY KEY,
    tenantid uuid NOT NULL REFERENCES accounts (id),
    name text NOT NULL CHECK (length(btrim(name)) > 0),
    CONSTRAINT rolestenantididuq UNIQUE (tenantid, id)
);

CREATE UNIQUE INDEX rolestenantnameciuq ON roles (tenantid, lower(name));

CREATE TABLE rolepermissions (
    id uuid PRIMARY KEY,
    tenantid uuid NOT NULL,
    roleid uuid NOT NULL,
    permission text NOT NULL CHECK (length(btrim(permission)) > 0),
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz,
    CONSTRAINT rolepermissionsrolefk FOREIGN KEY (tenantid, roleid) REFERENCES roles (tenantid, id),
    CONSTRAINT rolepermissionsgrantuq UNIQUE (tenantid, roleid, permission)
);

CREATE TABLE usertenants (
    id uuid PRIMARY KEY,
    userid uuid NOT NULL REFERENCES users (id),
    tenantid uuid NOT NULL REFERENCES accounts (id),
    roleid uuid NOT NULL,
    status integer NOT NULL CHECK (status IN (0, 1, 2)),
    jointime timestamptz NOT NULL,
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz,
    creatorid uuid NOT NULL REFERENCES users (id),
    lastmodifierid uuid REFERENCES users (id),
    CONSTRAINT usertenantsrolefk FOREIGN KEY (tenantid, roleid) REFERENCES roles (tenantid, id),
    CONSTRAINT usertenantstenantuseruq UNIQUE (tenantid, userid),
    CONSTRAINT usertenantsmodificationauditck CHECK ((lastmodifiedtime IS NULL) = (lastmodifierid IS NULL))
);

CREATE INDEX usertenantsuserididx ON usertenants (userid);

CREATE TABLE companies (
    id uuid PRIMARY KEY,
    tenantid uuid NOT NULL REFERENCES accounts (id),
    legalname text NOT NULL CHECK (length(btrim(legalname)) > 0),
    tradename text NOT NULL,
    federaltaxnumber text NOT NULL CHECK (length(btrim(federaltaxnumber)) > 0),
    status integer NOT NULL CHECK (status IN (0, 1)),
    creationtime timestamptz NOT NULL,
    lastmodifiedtime timestamptz,
    creatorid uuid NOT NULL REFERENCES users (id),
    lastmodifierid uuid REFERENCES users (id),
    CONSTRAINT companiesmodificationauditck CHECK ((lastmodifiedtime IS NULL) = (lastmodifierid IS NULL))
);

CREATE INDEX companiestenantididx ON companies (tenantid);

INSERT INTO schemamigrations (version, appliedtime) VALUES (1, now());

COMMIT;
