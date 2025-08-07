# Product Roadmap (Draft)

## Guiding Principles
- Multi-faith inclusivity & respectful customization.
- Tenant autonomy with safe defaults.
- Gradual scaling architecture (avoid premature microservices).
- Automation augments, never dictates.
- Privacy & transparency.

## Milestone 1: Foundation (Month 1-2)
Goal: A single faith organization can sign up, pick a taxonomy template, customize terminology, and invite members.
- Tenant registration & domain slug.
- Religion/Sect taxonomy selection UI.
- Terminology resolution engine (donation/tithe synonyms etc.).
- Auth (email/password, password reset, email verification).
- Role base set: SuperAdmin (global), TenantOwner, Leader, Contributor, Member, Guest.
- Basic schedule: create/list upcoming services/events.
- Content seed: Daily prayer/thought placeholder.
- Audit logging minimal.

## Milestone 2: Community Core (Month 3-4)
- Realtime chat (general + #announcements channel) with presence.
- Donation (Stripe one-time + recurring test mode) + ledger UI.
- Personal profile settings.
- Simple search (Postgres FTS) across content & events.
- Custom role labels per tenant.
- Basic notification dispatch (email + push placeholder).

## Milestone 3: Automation & Media (Month 5-6)
- Automation rules engine (trigger templates: new member, upcoming event reminder, donation thank-you).
- Daily prayer/thought rotation curated per taxonomy.
- Media upload (audio sermon) + storage + streaming page (raw, no transcoding yet).
- Enhanced scheduling (recurrence, categories, reminders).
- Access policy matrix UI per role.

## Milestone 4: Advanced Engagement (Month 7-8)
- Video pipeline (transcode + adaptive playback) via external service.
- Group study sessions (scheduled + chat thread binding).
- Donation statements & top donor/contributor leaderboard.
- Moderation tools (flag message/content, action queue).
- Multi-channel chat with threading.

## Milestone 5: Insights & Growth (Month 9-10)
- Analytics dashboard (attendance, donations trend, engagement heatmap).
- Recommendation engine v1 (popular lessons by taxonomy, time-window weighting).
- Export/import tenant configuration & templates.
- Localization (first non-English language pack).

## Milestone 6: Ecosystem & Marketplace (Month 11-12)
- Template marketplace (approved liturgy/study packs).
- Community contributed daily content with moderation & voting.
- API keys for 3rd-party extensions.

## Stretch / Future
- Live streaming (low-latency) events.
- Offline-first MAUI enhancements (sync queue).
- AI-assisted content drafting (with opt-in and safety filters).
- Federated identity between tenants (visitor pass).

## KPIs (Early)
- Time to create + launch first event (<10 min).
- Weekly Active Organizations.
- Chat messages per active user per week.
- Daily prayer/thought engagement rate (views/clicks).
- Donation conversion rate.

## Risk Mitigation
- Scope creep: maintain milestone boundaries.
- Template bias: gather advisory board across traditions.
- Moderation load: phased community reporting + priority queue.

## Dependencies & Sequencing Notes
- Chat depends on auth + tenancy.
- Automation depends on audit/event logging.
- Media pipeline depends on stable object storage + CDN.
- Marketplace depends on robust template versioning.
