using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temple.Domain.Scheduling;

namespace Temple.Infrastructure.Persistence.Configurations;

public class ScheduleEventConfiguration : IEntityTypeConfiguration<ScheduleEvent>
{
    public void Configure(EntityTypeBuilder<ScheduleEvent> builder)
    {
        builder.ToTable("schedule_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(250);
        builder.Property(e => e.Type).IsRequired().HasMaxLength(60);
        builder.HasIndex(e => new { e.TenantId, e.StartUtc });
    }
}
