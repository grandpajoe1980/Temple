namespace Temple.Application.Audit;

public interface IAuditWriter
{
    Task WriteAsync(Guid tenantId, Guid? actorUserId, string action, string entityType, string entityId, object? data, CancellationToken ct);
}
