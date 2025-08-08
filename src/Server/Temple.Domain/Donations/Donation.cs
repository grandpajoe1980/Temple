namespace Temple.Domain.Donations;

public class Donation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = "usd";
    public string? Provider { get; set; } // stripe
    public string? ProviderDonationId { get; set; }
    public bool Recurring { get; set; }
    public Guid? FinanceGoalId { get; set; } // optional link to goal
    public Guid? StewardshipFundId { get; set; } // optional link to designated fund
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedUtc { get; set; }
    public string Status { get; set; } = "pending"; // pending, succeeded, failed, canceled
    public string? ProviderDataJson { get; set; }
}
