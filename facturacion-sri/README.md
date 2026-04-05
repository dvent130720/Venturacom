# Facturación SRI - Microservices (.NET 8)

Arquitectura de microservicios orientada a SRI Ecuador con énfasis en seguridad para certificados digitales.

## Estructura

```text
facturacion-sri/
├─ docker-compose.yml
├─ FacturacionSRI.sln
└─ src/
   ├─ Shared.Contracts/
   ├─ IdentityService.Api/
   ├─ BillingService.Api/
   ├─ CertificateService.Api/
   │  ├─ Domain/
   │  ├─ Application/
   │  ├─ Infrastructure/
   │  └─ API/
   └─ SriService.Worker/
```

## Seguridad aplicada en Certificate Service

- Certificado `.p12` cifrado con AES-256 (clave derivada con PBKDF2 SHA-256 + salt + IV).
- Password cifrada por separado.
- Master key solo por variable de entorno (`Encryption__MasterKey` en base64).
- Endpoint interno protegido por `X-Internal-ApiKey`.
- Endpoint público nunca retorna certificado desencriptado.
- Auditoría de acciones sensibles (`upload`, `activate`, `deactivate`, `internal_read`).

## Flujo de firmado

1. Billing emite `FacturaCreadaEvent`.
2. SRI Worker consume evento.
3. SRI Worker solicita certificado activo al endpoint interno de Certificate Service.
4. Certificate Service desencripta internamente y responde solo a servicio autorizado.
5. Worker firma XML y continúa con proceso de autorización SRI.

## Endpoints clave (Certificate Service)

- `POST /certificates/upload` (multipart/form-data)
- `GET /certificates/{tenantId}`
- `POST /certificates/{id}/activate`
- `POST /certificates/{id}/deactivate`
- `GET /certificates/internal/{tenantId}/raw` (solo interno con API Key)

## Ejemplo request upload

```bash
curl -X POST 'http://localhost:8083/certificates/upload' \
  -H 'Authorization: Bearer <jwt>' \
  -F 'tenantId=11111111-1111-1111-1111-111111111111' \
  -F 'name=certificado-produccion-2026' \
  -F 'password=MiPasswordSecreta' \
  -F 'file=@firma.p12'
```

Respuesta:

```json
{
  "id": "3e6d19f9-3044-4af5-ab95-1471bf357631"
}
```

## Arranque

1. Exportar variables seguras:
   - `CERT_MASTER_KEY_BASE64`
   - `INTERNAL_API_KEY`
2. `docker compose up --build`

## Notas de producción

- Reemplazar autenticación demo de Identity por proveedor real (Keycloak/Entra/Auth0).
- Implementar migraciones EF (`dotnet ef migrations`).
- Sustituir publisher/consumer mock de RabbitMQ por cliente real.
- Mover secretos a Vault/KMS (AWS KMS, Azure Key Vault, Hashicorp Vault).
