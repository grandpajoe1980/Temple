namespace Temple.Application.Tenants;

// TaxonomyId should point to chosen sect (leaf) when provided; if only religion provided, service attempts to find default sect
public record TenantCreateRequest(string Name, string? TaxonomyId, string? ReligionId = null);
