# Tenant System Documentation

## Overview
The Temple application supports multi-tenant architecture where each tenant represents an organization (church, synagogue, temple, mosque, etc.).

## Features

### Tenant Creation
Users can create new tenants with the following options:
- **Organization Name** (required): The name of the organization
- **Religion/Faith** (optional): Select from available religions
- **Denomination/Sect** (optional): Select specific denomination within a religion

### Tenant Customization
After creation, tenant administrators can customize their tenant through the Settings page:
- Update organization name
- Change tenant status (active, suspended, archived)
- Modify religious affiliation
- View tenant slug (read-only identifier)

### Taxonomy System
The system uses a hierarchical taxonomy for religious organizations:
- **Religion Level**: Top-level religious categories (e.g., Christianity, Judaism, Islam)
- **Sect Level**: Specific denominations within a religion (e.g., Baptist, Reform Judaism)

Tenants can:
1. Have no religious affiliation (null taxonomy)
2. Be associated with a general religion
3. Be associated with a specific denomination/sect

## User Interface

### Creating a Tenant
1. Navigate to the Tenants page
2. Click "Create New Tenant"
3. Fill in the organization name
4. Optionally select a religion
5. If a religion is selected, optionally select a specific denomination
6. Click "Create Tenant"

### Customizing a Tenant
1. Navigate to the tenant dashboard
2. Click the "Settings" button in the quick actions
3. Modify any editable fields:
   - Organization name
   - Status
   - Religious affiliation (religion and/or sect)
4. Click "Save Settings"

## API Endpoints

### Public Endpoints
- `POST /api/v1/tenants` - Create a new tenant
- `GET /api/v1/tenants` - List all tenants
- `GET /api/v1/tenants/{id}` - Get tenant by ID
- `GET /api/v1/tenants/slug/{slug}` - Get tenant by slug
- `GET /api/v1/taxonomies/religions` - List available religions
- `GET /api/v1/taxonomies/religions/{id}/sects` - List sects for a religion

### Protected Endpoints (Require OrgManageSettings Capability)
- `GET /api/v1/tenants/{id}/settings` - Get tenant settings with terminology
- `PUT /api/v1/tenants/{id}` - Update tenant settings

## Technical Details

### Slug Generation
- Automatically generated from the tenant name
- Lowercase, hyphenated format
- Ensures uniqueness by appending numbers if needed (e.g., "my-church", "my-church-1")

### Status Values
- **active**: Tenant is fully operational
- **suspended**: Tenant has limited access (controlled by capabilities)
- **archived**: Tenant is read-only (historical data only)

### Data Model

```csharp
public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Slug { get; set; }
    public string Status { get; set; } // active, suspended, archived
    public string? TaxonomyId { get; set; } // Selected taxonomy node (sect or religion)
    public DateTime CreatedUtc { get; set; }
}
```

## Security

- Tenant creation is currently open (may require authentication in production)
- Tenant settings modification requires `OrgManageSettings` capability
- All operations require authentication via JWT Bearer token
- Tenant updates are audited for compliance and tracking

## Testing

Comprehensive test suite available in `src/Server/Temple.Tests/TenantTests.cs`:
- Tenant creation with various configurations
- Tenant retrieval by ID and slug
- Duplicate name handling
- Tenant listing
- Input validation

## Implementation Files

### Frontend
- `src/Client/Temple.Web/src/pages/CreateTenant.tsx` - Tenant creation page
- `src/Client/Temple.Web/src/pages/TenantSettings.tsx` - Tenant customization page
- `src/Client/Temple.Web/src/pages/TenantDashboard.tsx` - Main tenant dashboard
- `src/Client/Temple.Web/src/pages/Tenants.tsx` - Tenant list page
- `src/Client/Temple.Web/src/pages/App.tsx` - Application routing

### Backend
- `src/Server/Temple.Domain/Tenants/Tenant.cs` - Tenant entity
- `src/Server/Temple.Application/Tenants/` - Tenant application layer
- `src/Server/Temple.Infrastructure/Tenants/TenantService.cs` - Tenant business logic
- `src/Server/Temple.Api/Program.cs` - API endpoint definitions

### Tests
- `src/Server/Temple.Tests/TenantTests.cs` - Integration tests for tenant functionality

## Future Enhancements

Potential improvements for future releases:
- Tenant deletion with cascading rules
- Tenant transfer between users
- Custom terminology per tenant (already supported in backend)
- Tenant branding (colors, logo)
- Tenant-specific feature toggles
- Multi-language support per tenant
- Tenant analytics and usage metrics
