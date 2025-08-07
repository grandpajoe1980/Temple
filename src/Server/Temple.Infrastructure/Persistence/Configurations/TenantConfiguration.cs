using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temple.Domain.Tenants;

namespace Temple.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Slug).IsRequired().HasMaxLength(120);
        builder.HasIndex(t => t.Slug).IsUnique();
        builder.Property(t => t.Status).IsRequired().HasMaxLength(40);
        builder.Property(t => t.CreatedUtc).IsRequired();
    }
}
