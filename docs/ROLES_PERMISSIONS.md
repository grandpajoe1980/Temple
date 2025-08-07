# Roles & Permissions Model (Draft)

## Global vs Tenant Scope
- Global (SuperAdmin): Manages platform-wide taxonomy, templates, abuse reports, plans.
- Tenant Scoped: Roles apply only within a single organization (church, temple, synagogue).

## Base System Roles (Immutable Keys)
| Key | Default Label | Scope | Description |
|-----|---------------|-------|-------------|
| super_admin | Super Admin | Global | Platform governance |
| tenant_owner | Founder | Tenant | Created the org; full control |
| leader | Leader | Tenant | Spiritual/organizational head (Priest, Rabbi, Abbot etc.) |
| contributor | Contributor | Tenant | Can create/edit most content within allowed categories |
| member | Member | Tenant | Authenticated participant |
| guest | Guest | Tenant | Limited read-only (public areas) |

Tenants can override Display Label per role (e.g., Leader -> Rabbi, Abbot) and define additional custom roles deriving from capability sets.

## Capability Matrix (Sample Subset)
| Capability Key | Description | Default Roles |
|----------------|-------------|---------------|
| org.manage.settings | Change tenant settings | tenant_owner, leader |
| org.manage.roles | Create custom roles | tenant_owner |
| org.view.audit | View audit logs | tenant_owner |
| content.create.lesson | Create lesson/study content | contributor, leader |
| content.publish.lesson | Publish lesson content | leader |
| schedule.create.event | Create service/event | contributor, leader |
| schedule.manage.event | Edit/delete any event | leader |
| chat.post.message | Post messages | member+ |
| chat.moderate | Delete/flag messages | leader, contributor (if granted) |
| donation.view.summary | View donation dashboard | tenant_owner, leader |
| donation.manage.refund | Initiate refund | tenant_owner |
| automation.manage.rules | Create automation rules | tenant_owner, leader |
| media.upload | Upload media assets | contributor, leader |
| media.manage | Delete/retire media | leader |

## Custom Role Creation Flow
1. Tenant selects base role (template) or starts empty.
2. Assigns capabilities (checkbox list grouped by domain).
3. Provides display label & optional localized synonyms.
4. Saves -> versioned snapshot stored (role_version table) for audit.

## Enforcement Strategy
- Capabilities mapped to authorization policies in API.
- JWT includes capability list hash + role version timestamp.
- Middleware validates hash; if mismatch (role changed) forces re-issue.

## Terminology Overrides
Example resolved dictionary for a Baptist tenant:
```
{
  "donation": "tithe",
  "leader": "Pastor",
  "service": "Worship Service"
}
```
Overrides do not alter capability keys—only display labels.

## Audit Events (Examples)
- ROLE_UPDATED
- CAPABILITY_GRANTED
- TERMINOLOGY_OVERRIDE_SET
- DONATION_REFUND_ISSUED
- CONTENT_PUBLISHED

## Open Questions
- Should guests be allowed to post in specific channels? (Default no)
- Soft deletion retention policy length?
- Versioning strategy for capability additions (backward compat)?
