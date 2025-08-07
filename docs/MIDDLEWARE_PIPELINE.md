# Middleware & Pipeline (Draft)

## Order (Tentative)
1. Serilog request logging
2. Correlation / Trace Id Assignment
3. Exception Handling (problem details serializer)
4. Security Headers (CSP, X-Frame-Options, etc.)
5. Rate Limiter (future)
6. Tenant Resolution (Domain from host -> TenantId)
7. Authentication (JWT) 
8. Authorization (policy & capability evaluation)
9. Localization / Terminology Injection
10. Caching / ETag (for GET endpoints) (future)
11. Endpoint Execution
12. Response Compression

## Tenant Resolution
- Strategy: Subdomain (slug.example.com) else X-Tenant-Slug header else query param fallback (development only).
- Caches slug->TenantId in Redis (TTL 5m, version bump on tenant update).

## Exception Handling
- Map domain exceptions to typed problem codes.
- Log full details with traceId; user receives sanitized message.

## Authentication
- JWT access tokens (15m) + refresh tokens (rotating, 30d) stored HTTP-only cookie (web) or secure storage (mobile).

## Authorization
- Capabilities list hashed inside token claims (cap_hash). If role version changes, middleware rejects with 401 requiring re-auth.

## Observability
- OpenTelemetry instrumentation for HTTP server, EF Core, Redis.
- Correlation Id propagated via TraceParent header.

## Security Headers (Baseline)
- Strict-Transport-Security: max-age=63072000; includeSubDomains; preload
- Content-Security-Policy: default-src 'self'; frame-ancestors 'none'
- X-Content-Type-Options: nosniff
- X-Frame-Options: DENY
- Referrer-Policy: no-referrer

## Rate Limiting (Planned)
- Sliding window counters keyed by tenant + ip + route group.

## Body Size Limits
- Default 10MB; media uploads go to a dedicated pre-signed upload endpoint.
