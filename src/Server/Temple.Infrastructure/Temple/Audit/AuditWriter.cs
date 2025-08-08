using System.Text.Json;
using Temple.Application.Audit;
using Temple.Domain.Audit;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Audit;

public class AuditWriter : IAuditWriter
{
    private readonly AppDbContext _db;
    public AuditWriter(AppDbContext db) => _db = db;

    public async Task WriteAsync(Guid tenantId, Guid? actorUserId, string action, string entityType, string entityId, object? data, CancellationToken ct)
    {
        var evt = new AuditEvent
        {
            TenantId = tenantId,
            ActorUserId = actorUserId ?? Guid.Empty,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DataJson = data == null ? null : JsonSerializer.Serialize(data)
        };
        _db.AuditEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
    }
}
