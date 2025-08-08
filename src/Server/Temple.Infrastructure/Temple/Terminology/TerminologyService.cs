using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Temple.Application.Terminology;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Terminology;

public class TerminologyService : ITerminologyService
{
    private readonly AppDbContext _db;
    private static readonly IReadOnlyDictionary<string,string> GlobalDefaults = new Dictionary<string,string>();
    private static readonly MemoryCache Cache = new(new MemoryCacheOptions { SizeLimit = 10_000 });

    public TerminologyService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyDictionary<string, string>> GetResolvedAsync(Guid tenantId, string? taxonomyId, CancellationToken ct = default)
    {
        var key = $"term:{tenantId}:{taxonomyId}";
        if (Cache.TryGetValue(key, out IReadOnlyDictionary<string,string>? cached)) return cached!;
        var layers = new List<Dictionary<string,string>> { new(GlobalDefaults) };
        if (!string.IsNullOrWhiteSpace(taxonomyId))
        {
            var currentId = taxonomyId;
            var stack = new Stack<Dictionary<string,string>>();
            while (!string.IsNullOrWhiteSpace(currentId))
            {
                var node = await _db.ReligionTaxonomies.FirstOrDefaultAsync(t => t.Id == currentId, ct);
                if (node == null) break;
                if (!string.IsNullOrWhiteSpace(node.DefaultTerminologyJson))
                {
                    var dict = JsonSerializer.Deserialize<Dictionary<string,string>>(node.DefaultTerminologyJson) ?? new();
                    stack.Push(dict);
                }
                currentId = node.ParentId;
            }
            while (stack.Count > 0) layers.Add(stack.Pop());
        }
        var overrideEntity = await _db.TerminologyOverrides.Where(o => o.TenantId == tenantId)
            .OrderByDescending(o => o.CreatedUtc).FirstOrDefaultAsync(ct);
        if (overrideEntity != null)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string,string>>(overrideEntity.OverridesJson) ?? new();
            layers.Add(dict);
        }
        var merged = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in layers)
            foreach (var kvp in layer)
                merged[kvp.Key] = kvp.Value;

        Cache.Set(key, merged, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10), Size = merged.Count });
        return merged;
    }
}
