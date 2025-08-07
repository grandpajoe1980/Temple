# API Routing & Versioning (Draft)

## Versioning Strategy
- Prefix: /api/v1 for public stable endpoints.
- Internal /admin or /internal endpoints gated by capability & not guaranteed stable.
- Future: Introduce /api/v2 only when breaking changes accumulate; support overlap period.

## Current Minimal Endpoints
| Method | Path | Description | Auth | Notes |
|--------|------|-------------|------|-------|
| GET | /health | Liveness/health | None | K8s probe safe |
| GET | /api/tenants/{id} | Fetch tenant basic info | Tenant-scoped | Returns 404 if not found |
| POST | /api/tenants | Create tenant | Public (temp) | Slug auto-generated |

## Planned MVP Endpoints
| Method | Path | Description | Auth Capability |
|--------|------|-------------|------------------|
| POST | /api/tenants | Create tenant | (public) create_tenant |
| GET | /api/tenants/slug/{slug} | Resolve tenant by slug | tenant.read |
| POST | /api/auth/register | User registration | public |
| POST | /api/auth/login | Issue JWT | public |
| POST | /api/auth/refresh | Refresh tokens | auth.refresh |
| GET | /api/users/me | Current profile | user.read.self |
| POST | /api/schedule/events | Create event | schedule.create.event |
| GET | /api/schedule/events | List events | schedule.read |
| POST | /api/chat/channels | Create channel | chat.manage.channel |
| GET | /api/chat/channels | List channels | chat.read |
| GET | /api/chat/channels/{id}/messages | List messages | chat.read |
| POST | /api/chat/channels/{id}/messages | Post message | chat.post.message |
| POST | /api/donations/initiate | Begin donation (Stripe) | donation.create |
| POST | /api/donations/webhook/stripe | Stripe webhook | (unauth signed) |
| GET | /api/donations/summary | Donation dashboard | donation.view.summary |
| GET | /api/automation/rules | List automation rules | automation.manage.rules |
| POST | /api/automation/rules | Create rule | automation.manage.rules |

## Naming Conventions
- Nouns plural (events, channels, donations).
- Use POST for actions; avoid verbs in path except subordinate actions (/cancel if necessary).

## Error Model (Draft)
```
{
  "traceId": "...",
  "error": {
    "code": "TENANT_NOT_FOUND",
    "message": "Tenant not found",
    "details": {}
  }
}
```

## Pagination
- Query params: ?page=1&pageSize=50 (defaults 1/20; max 100).
- Response envelope includes: { data: [...], page, pageSize, total }

## Filtering
- Provide simple field filters (?status=active). Complex filters via POST /search endpoints later.

## Authentication Headers
- Authorization: Bearer <token>
- X-Tenant-Slug for unauthenticated resolution when needed (pre-login flows).

## Rate Limiting (Planned)
- Per-IP & per-tenant policy (Sliding window) enforced by middleware + Redis counters.
