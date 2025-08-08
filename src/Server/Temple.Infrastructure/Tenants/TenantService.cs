using Microsoft.EntityFrameworkCore;
using Temple.Application.Tenants;
using Temple.Domain.Shared;
using Temple.Domain.Tenants;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Tenants;

public class TenantService : ITenantService
{
    private readonly AppDbContext _db;

    public TenantService(AppDbContext db) => _db = db;

    public async Task<Tenant> CreateAsync(TenantCreateRequest request, CancellationToken ct = default)
    {
        var slugBase = Slug.From(request.Name);
        if (string.IsNullOrWhiteSpace(slugBase)) throw new ArgumentException("Invalid tenant name");

        var slug = slugBase;
        var i = 1;
        while (await _db.Tenants.AnyAsync(t => t.Slug == slug, ct))
        {
            slug = $"{slugBase}-{i++}";
        }

        string? taxonomyId = request.TaxonomyId;
        if (taxonomyId == null && !string.IsNullOrWhiteSpace(request.ReligionId))
        {
            // Attempt to pick first sect under religion
            taxonomyId = await _db.ReligionTaxonomies
                .Where(t => t.ParentId == request.ReligionId && t.Type == "sect")
                .OrderBy(t => t.DisplayName)
                .Select(t => t.Id)
                .FirstOrDefaultAsync(ct);
            taxonomyId ??= request.ReligionId; // fallback to religion node if no sects
        }

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Slug = slug,
            TaxonomyId = taxonomyId
        };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);
        return tenant;
    }
}
