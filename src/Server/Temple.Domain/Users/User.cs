namespace Temple.Domain.Users;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public string? DisplayName { get; set; }
    public DateTime? EmailVerifiedUtc { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiresUtc { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiresUtc { get; set; }
    public bool IsSuperAdmin { get; set; } // platform-wide governance
    public bool IsGuest { get; set; } // guest / anonymous session user
}
