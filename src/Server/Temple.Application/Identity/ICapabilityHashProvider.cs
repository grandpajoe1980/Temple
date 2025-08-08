namespace Temple.Application.Identity;

public interface ICapabilityHashProvider
{
    Task<string> GetForTenantAsync(Guid tenantId, CancellationToken ct);
    Task<bool> ValidateAsync(Guid tenantId, string? providedHash, CancellationToken ct);
}
