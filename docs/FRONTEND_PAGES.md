# Frontend Pages & Screens (Draft)

## Web (Next.js Planned)
| Route | Purpose | Auth | Notes |
|-------|---------|------|-------|
| / | Marketing landing, sign-up CTA | Public | SEO optimized |
| /login | Login form | Public | Magic link optional later |
| /register | Tenant + user creation wizard | Public | Multi-step (tenant, taxonomy, admin) |
| /dashboard | Org overview metrics | Auth (member+) | Tenant context required |
| /settings/terminology | Customize labels | tenant_owner | Live preview |
| /settings/roles | Manage custom roles | tenant_owner | Capability matrix editor |
| /schedule | List events/services | member+ | Filters by category |
| /schedule/new | Create event | contributor+ | Recurrence later |
| /content/lessons | Lessons library | member+ | Tag filtering |
| /content/lessons/new | Create lesson | contributor+ | Draft flow |
| /chat | Channel list + messages | member+ | WebSocket/SignalR |
| /donations | Donation CTA + history | member+ | Payment elements |
| /donations/admin | Summary & exports | donation.view.summary | Charts |
| /automation | Rules list | leader+ | CRUD |
| /media | Media library | contributor+ | Upload -> processing states |
| /leaderboard/donors | Top donors | member+ | Cache 5m |
| /leaderboard/contributors | Top content contributors | member+ | Weight algorithm |

## Mobile (MAUI) Initial Tabs
| Tab | Screen | Purpose |
|-----|--------|---------|
| Home | DashboardSummaryPage | Key metrics + daily thought/prayer |
| Schedule | ScheduleListPage | Upcoming events |
| Chat | ChatChannelsPage | Real-time communication |
| Content | LessonsListPage | Study/lesson content |
| More | SettingsDrawerPage | Terminology, roles (if permitted) |

## Mobile Onboarding Flow
1. Welcome / marketing highlights.
2. Login / Register (option choose existing tenant or create new).
3. If new tenant: taxonomy selection -> terminology preview -> confirm.
4. Land on dashboard with initial suggestions.

## Component Library (Shared UI Goals)
- Typography scale with religious-neutral defaults; allow custom theming per tenant later.
- Icon set: Fluent or Feather as baseline.

## Internationalization
- All user-facing strings through i18n keys; tenant override layer for terminology.

## Accessibility Targets
- WCAG 2.1 AA for web.
- Dynamic font size support mobile.
- Color contrast validated for default theme.
