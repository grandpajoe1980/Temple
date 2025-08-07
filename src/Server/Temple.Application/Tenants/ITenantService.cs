using Temple.Domain.Tenants;

namespace Temple.Application.Tenants;

public interface ITenantService
{
    Task<Tenant> CreateAsync(TenantCreateRequest request, CancellationToken ct = default);
}
