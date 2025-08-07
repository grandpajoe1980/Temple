using Microsoft.EntityFrameworkCore;
using Temple.Domain.Tenants;
using Temple.Domain.Users;
using Temple.Domain.Content;
using Temple.Domain.Scheduling;

namespace Temple.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {}

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<ScheduleEvent> ScheduleEvents => Set<ScheduleEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
