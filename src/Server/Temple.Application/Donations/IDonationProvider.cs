namespace Temple.Application.Donations;

public interface IDonationProvider
{
    Task<InitiateDonationResult> InitiateAsync(DonationInitiateRequest request, CancellationToken ct);
    Task HandleWebhookAsync(string body, IReadOnlyDictionary<string,string> headers, CancellationToken ct);
}

public record DonationInitiateRequest(Guid TenantId, Guid? UserId, long AmountCents, string Currency, bool Recurring);
public record InitiateDonationResult(string Provider, string ProviderPaymentId, string RedirectUrl); // keep outward contract (rename internal model field only)
