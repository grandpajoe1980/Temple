namespace Temple.Domain.Finance;

public class RecurringCommitment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; } // person making commitment
    public long AmountCents { get; set; }
    public string Frequency { get; set; } = "monthly"; // weekly, biweekly, monthly
    public Guid? FinanceGoalId { get; set; }
    public Guid? StewardshipFundId { get; set; }
    public string? Notes { get; set; }
    public DateTime StartUtc { get; set; } = DateTime.UtcNow;
    public DateTime? EndUtc { get; set; }
    public bool Active { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
