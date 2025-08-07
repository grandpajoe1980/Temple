using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temple.Domain.Content;

namespace Temple.Infrastructure.Persistence.Configurations;

public class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Title).IsRequired().HasMaxLength(300);
        builder.Property(l => l.Body).IsRequired();
        builder.Property(l => l.Tags).HasColumnType("text[]");
        builder.HasIndex(l => new { l.TenantId, l.PublishedUtc });
    }
}
