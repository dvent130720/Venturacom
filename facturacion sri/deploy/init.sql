CREATE TABLE IF NOT EXISTS certificates (
    id uuid PRIMARY KEY,
    tenant_id uuid NOT NULL,
    name varchar(200) NOT NULL,
    encrypted_p12 bytea NOT NULL,
    encrypted_password bytea NOT NULL,
    thumbprint varchar(200),
    expiration_date timestamptz NOT NULL,
    is_active boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_certificates_tenant_id ON certificates(tenant_id);
CREATE INDEX IF NOT EXISTS idx_certificates_is_active ON certificates(is_active);
