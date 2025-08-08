namespace Temple.Domain.Finance;

public class FinanceGoal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty; // e.g., general_fund_annual, missions_q3
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal CurrentAmount { get; set; } // optional manual tracking (else derived from ledger/donations)
    public DateTime StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
