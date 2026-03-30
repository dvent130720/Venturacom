# Venturacom Backend

Backend SaaS multi-tenant para facturación electrónica (SRI Ecuador) con .NET 8 y Clean Architecture.

## Contexto para Codex (futuro)
Este repositorio está preparado para que un agente (Codex) pueda continuar el desarrollo sin perder contexto funcional y técnico.

### Objetivo del sistema
- Plataforma multi-tenant (`tenant_id`) para facturación electrónica.
- Seguridad fuerte: JWT + refresh token persistente y rotación.
- Procesamiento asíncrono de facturas hacia SRI vía worker.
- Auditoría XML de request/response SRI.
- Escalabilidad horizontal.

## Arquitectura
Estructura por capas:

- `src/Venturacom.Domain`
  - Entidades, enums y reglas de dominio base.
- `src/Venturacom.Application`
  - Casos de uso (MediatR), DTOs, validaciones (FluentValidation), abstracciones.
- `src/Venturacom.Infrastructure`
  - EF Core, auth providers, Redis cache distribuido, cola SQL, worker, integración Google/Loki.
- `src/Venturacom.API`
  - Controllers, middlewares, bootstrapping, seguridad HTTP, rate limiting.

## Componentes críticos

### Multi-tenant
- `TenantMiddleware` inyecta tenant en `ITenantContext` para requests autenticadas.
- `ApplicationDbContext` aplica query filter por `TenantId` para entidades multi-tenant.

### Autenticación
- Endpoints:
  - `POST /api/auth/login`
  - `POST /api/auth/register`
  - `POST /api/auth/google`
  - `POST /api/auth/refresh`
- Refresh tokens se guardan **hasheados** (SHA-256) en base de datos.

### Worker y cola
- `InvoiceWorker` procesa jobs asíncronos de `invoice_jobs`.
- `SqlInvoiceQueue` reclama jobs con locking fino (`ROWLOCK`, `READPAST`, `UPDLOCK`) para evitar colisiones entre réplicas.

### Observabilidad
- Logs por Serilog a consola y Loki (`Loki:Url`).
- Trazabilidad SRI en tabla `sri_logs`.

## Configuración mínima
Configurar en `src/Venturacom.API/appsettings.json`:

- `ConnectionStrings:DefaultConnection`
- `ConnectionStrings:Redis`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:Key`
- `GoogleAuth:ClientId`
- `Loki:Url`

## Convenciones para futuras tareas de Codex
- Mantener separación por capa (sin lógica de negocio en controllers).
- Todo acceso a datos multi-tenant debe respetar `TenantId`.
- Usar async/await extremo a extremo.
- Validar comandos con FluentValidation.
- No introducir secretos hardcodeados.
- Preferir cambios incrementales con pruebas por módulo.

## Próximos pasos recomendados
1. Añadir migraciones EF Core y scripts de despliegue.
2. Añadir pruebas unitarias e integración (auth, tenant isolation, worker).
3. Sustituir `SriXmlService` simulado por cliente real SRI.
4. Añadir OpenTelemetry (traces + metrics) y dashboards.
5. Endurecer políticas de bloqueo/rate-limit por endpoint sensible.

## Comandos de desarrollo (cuando exista .NET SDK)
```bash
dotnet restore Venturacom.sln
dotnet build Venturacom.sln
dotnet test Venturacom.sln
```
