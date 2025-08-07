# Architecture Overview

## Vision
A multi-tenant, religion-agnostic community & communications platform supporting web (PWA), iOS, Android, and optionally desktop. It enables any faith community (church, temple, synagogue, monastery, etc.) to create a customizable digital hub including teaching content, schedules, chat, media, donations, and automation-driven engagement.

## High-Level Platform Components
- **Client Apps**
  - Mobile: .NET MAUI (iOS / Android) leveraging shared UI + native capabilities (notifications, background tasks, media capture).
  - Web: Next.js (React) + PWA features for installability and offline caching.
  - Admin Portal: Web-based (Next.js) with role & permissions management, templating, analytics.
- **API Layer**
  - Backend: .NET (ASP.NET Core minimal APIs or modular monolith) or NestJS (Node) — decision pending; initial bias toward ASP.NET Core for synergy with MAUI.
  - GraphQL (HotChocolate) or REST + SignalR (realtime) for chat, live events, presence.
- **Multi-Tenancy Strategy**
  - Single database (pooled) with TenantId discriminator to start.
  - Option to graduate to schema-per-tenant or database-per-tenant for large premium orgs.
  - Tenant resolution: subdomain (orgslug.domain) + explicit header fallback + user claims.
- **Data Storage**
  - Primary: PostgreSQL (roles, org structures, schedules, content metadata, donations records).
  - Search / Recommendation: OpenSearch or Azure Cognitive Search (later phase).
  - Caching: Redis (session, rate limiting, feed personalization queues).
  - Object Storage: S3 / Azure Blob (media, documents, recorded sermons/videos).
- **Media Pipeline**
  - Ingest -> Transcode (FFmpeg in container or cloud service) -> Store -> CDN edge delivery (CloudFront / Azure CDN) -> Player with adaptive bitrate (HLS/DASH).
- **Authentication & Identity**
  - External: Email/password + SSO (Apple, Google) + optional Magic Link.
  - Identity Provider: Auth0 / Azure AD B2C / Supabase Auth / Cognito (choose one). Start with ASP.NET Core Identity + JWT + refresh tokens if self-managed.
  - Roles & Custom Role Labels per tenant.
- **Payments & Donations**
  - Phase 1: Stripe primary (recurring, one-time, pledges; terminology mapping for tithe/offering per template).
  - Pluggable provider interface enabling later providers: PayPal, Zelle (manual/async confirmation), Venmo (via PayPal APIs), Cash App, Bitcoin (BTCPay) based on demand signals.
  - Webhooks -> Donation ledger -> Acknowledgment workflows -> Automation triggers (e.g., DonationReceived).
- **Automation & Personalization Engine**
  - Rule definitions (When Event THEN Action) + Scheduled tasks (Quartz.NET / Hangfire).
  - Content suggestion model (popularity, recency, tenant taxonomy).
- **Template & Localization Service**
  - Template definitions: JSON-based seed + override layers (Global -> Religion -> Sect -> Tenant custom).
  - Localization & Terminology dictionary (e.g., donation vs tithe) via i18n resource files + dynamic term mapping.
- **Notifications**
  - Push (FCM/APNS), Email (SendGrid), In-app, SMS (Twilio optional).
- **Analytics & Insights**
  - Event pipeline (OpenTelemetry -> Kafka / Event Hub -> Warehouse -> Dashboard via Metabase / Superset).

## Domain Bounded Contexts
- Identity & Access (Users, Roles, Permissions, Sessions)
- Tenancy & Templates (Organizations, Religious Taxonomy, Terminology Overrides)
- Scheduling (Services, Events, Recurring Patterns)
- Content (Lessons, Readings, Sermons, Media References)
- Chat & Community (Channels, Threads, Messages, Reactions, Presence)
- Donations & Finance (Donations, Pledges, Subscriptions, Statements)
- Automation & Recommendations (Rules, Jobs, Suggestions, Daily Prayer/Thought)
- Media Processing (Ingest, Transcode, Asset Delivery)
- Notifications (Dispatch, Preferences, Subscriptions)

## Key Cross-Cutting Concerns
- Authorization: Policy-based + resource ownership + role capability matrix stored per tenant.
- Audit Trail: Immutable event log for sensitive actions.
- Rate Limiting & Abuse Prevention: Per-tenant + per-user quotas.
- Observability: Structured logs (Serilog), metrics (Prometheus), tracing (OTel).
- Privacy & Compliance: GDPR consent flows for EU tenants, data export, right to forget.

## Tech Stack (Proposed Initial)
| Layer | Choice (Initial) | Rationale |
|-------|------------------|-----------|
| Mobile | .NET MAUI | Shared logic, faster iOS/Android delivery |
| Web | Next.js (React) | SEO + PWA + rich admin portal |
| API | ASP.NET Core | Strong typing, performance, synergy |
| Realtime | SignalR | Integrated with ASP.NET Core |
| DB | PostgreSQL | JSONB flexibility, reliability |
| Cache | Redis | Session + ephemeral state |
| Media | S3 + CloudFront | Standard pattern |
| Auth | ASP.NET Core Identity (phase 1) | Speed, later externalize |
| Payments | Stripe | Robust donation/billing |
| Jobs | Hangfire | Dashboard + persistence |
| Infra | Docker + Terraform | Portability & reproducibility |
| IaC Cloud | AWS (phase 1) | Broad service support |

## Multi-Tenant Data Model (Sketch)
Tables include a TenantId (UUID) except Tenants & ReligionTaxonomy.
```
Tenants(TenantId, Name, Slug, Status, PlanTier, CreatedUtc)
ReligionTaxonomy(Id, ParentId, Type, Slug, DisplayName, CanonicalTexts[], DefaultTerminologyJson)
TenantTemplateOverrides(Id, TenantId, TaxonomyId, OverridesJson)
Users(UserId, Email, HashedPassword, ...)
TenantUsers(TenantId, UserId, RoleKey, CustomRoleLabel, CapabilitiesJson)
Capabilities(CapabilityKey, Description, DefaultRoleMatrixJson)
ContentItems(Id, TenantId, Type, Title, Body, Tags[], Visibility, PublishedUtc)
Schedules(Id, TenantId, Title, StartUtc, EndUtc, RecurrenceRule, Type)
ChatChannels(Id, TenantId, Type, Name, Visibility)
Messages(Id, ChannelId, TenantId, UserId, Body, RichPayloadJson, CreatedUtc)
Donations(Id, TenantId, UserId, Amount, Currency, RecurringId?, CreatedUtc, MetaJson)
AutomationRules(Id, TenantId, TriggerType, ConditionJson, ActionJson, Enabled)
MediaAssets(Id, TenantId, Type, StorageKey, Status, TranscodeProfile, CreatedUtc)
NotificationPreferences(Id, TenantId, UserId, ChannelsJson)
AuditEvents(Id, TenantId, ActorUserId, Action, EntityType, EntityId, DataJson, CreatedUtc)
```

## Template & Terminology Resolution
Order of precedence (highest wins): Tenant Custom > Sect > Religion > Global Default.
Runtime composition caches a resolved dictionary: `{ "donation": "tithe", "leaderTitle": "Abbot" }`.

## Initial Milestone Slice (Foundational Vertical)
1. Tenant sign-up + taxonomy selection (Baptist / Theravada / Reform Judaism etc.)
2. Terminology mapping & default seed content (daily prayer placeholder).
3. Auth (register/login, email verify) + role assignment (SuperAdmin global; Tenant Creator local).
4. Basic schedule (create service) + list view.
5. Chat MVP (global general channel per tenant) realtime.
6. Donation (Stripe one-time test mode) + ledger.
7. Automation: Daily “Thought/Prayer” suggestion from curated pool per taxonomy.

## Evolution Path
- v2: Custom roles UI, multi-channel chat, recurring events, media upload.
- v3: Video transcoding pipeline, recommendation scoring, analytics dashboards.
- v4: Marketplace for shared liturgical packs & study guides.

## Security Considerations
- All tenant user operations require TenantId scoping.
- Row-Level Security (optional) if using PostgreSQL RLS.
- JWT tokens embed TenantIds & capability claims; short-lived access + refresh rotation.

## Open Questions
See PROJECT_DECISIONS.md.
