using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Temple.Application.Identity;
using Temple.Domain.Identity;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Identity;

public class CapabilityHashProvider : ICapabilityHashProvider
{
    private readonly AppDbContext _db;
    private readonly ILogger<CapabilityHashProvider> _logger;
    public CapabilityHashProvider(AppDbContext db, ILogger<CapabilityHashProvider> logger)
    { _db = db; _logger = logger; }

    public async Task<string> GetForTenantAsync(Guid tenantId, CancellationToken ct)
    {
        var latest = await _db.RoleVersions.Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.Version).FirstOrDefaultAsync(ct);
        if (latest != null) return latest.CapabilityHash;
        // Generate initial snapshot hash from static RoleCapabilities for now
        var map = new Dictionary<string,string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [RoleCapabilities.TenantOwner] = RoleCapabilities.Get(RoleCapabilities.TenantOwner).ToArray(),
            [RoleCapabilities.Leader] = RoleCapabilities.Get(RoleCapabilities.Leader).ToArray(),
            [RoleCapabilities.Contributor] = RoleCapabilities.Get(RoleCapabilities.Contributor).ToArray(),
            [RoleCapabilities.Member] = RoleCapabilities.Get(RoleCapabilities.Member).ToArray(),
            [RoleCapabilities.Guest] = RoleCapabilities.Get(RoleCapabilities.Guest).ToArray()
        };
        var serialized = System.Text.Json.JsonSerializer.Serialize(map);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(serialized)));
        var version = new RoleVersion { TenantId = tenantId, Version = 1, CapabilityHash = hash };
        _db.RoleVersions.Add(version);
        await _db.SaveChangesAsync(ct);
        return hash;
    }

    public async Task<bool> ValidateAsync(Guid tenantId, string? providedHash, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providedHash)) return false;
        var current = await GetForTenantAsync(tenantId, ct);
        return string.Equals(current, providedHash, StringComparison.OrdinalIgnoreCase);
    }
}
