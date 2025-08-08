using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Temple.Domain.Users;

namespace Temple.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
    builder.Property(u => u.TenantId).IsRequired();
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
    builder.HasIndex(u => u.Email).IsUnique();
    builder.HasIndex(u => new { u.TenantId, u.Email });
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.DisplayName).HasMaxLength(200);
        builder.Property(u => u.PasswordResetToken).HasMaxLength(200);
        builder.Property(u => u.VerificationToken).HasMaxLength(200);
    }
}
