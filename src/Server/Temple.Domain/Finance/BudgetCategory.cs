namespace Temple.Domain.Finance;

public class BudgetCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public string Key { get; set; } = string.Empty; // unique per tenant
    public string Name { get; set; } = string.Empty;
    public string? PeriodKey { get; set; } // e.g. FY2025, 2025-Q1, or custom
    public long BudgetAmountCents { get; set; }
    public long ActualAmountCents { get; set; } // denormalized from approved expenses
    public bool IsArchived { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
