namespace Temple.Application.Identity;

public interface ICapabilityHashRegenerator
{
    Task RegenerateAsync(Guid tenantId, CancellationToken ct);
}
