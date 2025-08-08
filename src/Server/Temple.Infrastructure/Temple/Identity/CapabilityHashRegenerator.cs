using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Temple.Application.Identity;
using Temple.Domain.Identity;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Identity;

public class CapabilityHashRegenerator : ICapabilityHashRegenerator
{
    private readonly AppDbContext _db;
    private readonly ILogger<CapabilityHashRegenerator> _logger;
    public CapabilityHashRegenerator(AppDbContext db, ILogger<CapabilityHashRegenerator> logger)
    { _db = db; _logger = logger; }

    public async Task RegenerateAsync(Guid tenantId, CancellationToken ct)
    {
        var map = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleCapabilities.TenantOwner] = RoleCapabilities.Get(RoleCapabilities.TenantOwner).ToArray(),
            [RoleCapabilities.Leader] = RoleCapabilities.Get(RoleCapabilities.Leader).ToArray(),
            [RoleCapabilities.Contributor] = RoleCapabilities.Get(RoleCapabilities.Contributor).ToArray(),
            [RoleCapabilities.Member] = RoleCapabilities.Get(RoleCapabilities.Member).ToArray(),
            [RoleCapabilities.Guest] = RoleCapabilities.Get(RoleCapabilities.Guest).ToArray()
        };
        var custom = await _db.CustomRoles.Where(r => r.TenantId == tenantId).ToListAsync(ct);
        foreach (var c in custom)
        {
            map[c.Key] = c.Capabilities.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        }
        var serialized = System.Text.Json.JsonSerializer.Serialize(map);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));
        var latest = await _db.RoleVersions.Where(r => r.TenantId == tenantId).OrderByDescending(r => r.Version).FirstOrDefaultAsync(ct);
        var nextVersion = (latest?.Version ?? 1) + 1;
        var version = new RoleVersion { TenantId = tenantId, Version = nextVersion, CapabilityHash = hash };
        _db.RoleVersions.Add(version);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Regenerated capability hash for tenant {TenantId} version {Version}", tenantId, nextVersion);
    }
}
