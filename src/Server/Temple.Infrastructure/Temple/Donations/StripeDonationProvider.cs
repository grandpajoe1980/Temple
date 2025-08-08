using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Temple.Application.Donations;
using Temple.Infrastructure.Persistence;
using Temple.Domain.Donations;

namespace Temple.Infrastructure.Temple.Donations;

public class StripeOptions
{
    public string? ApiKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string? SuccessUrl { get; set; }
    public string? CancelUrl { get; set; }
}

public class StripeDonationProvider : IDonationProvider
{
    private readonly StripeOptions _options;
    private readonly AppDbContext _db;
    private readonly ILogger<StripeDonationProvider> _logger;
    public StripeDonationProvider(IConfiguration config, AppDbContext db, ILogger<StripeDonationProvider> logger)
    {
        _options = config.GetSection("Stripe").Get<StripeOptions>() ?? new StripeOptions();
        _db = db; _logger = logger;
    }

    public Task<InitiateDonationResult> InitiateAsync(DonationInitiateRequest request, CancellationToken ct)
    {
        // Placeholder: in real integration use Stripe SDK to create CheckoutSession or PaymentIntent
        var paymentId = Guid.NewGuid().ToString("N");
        var redirect = ($"{_options.SuccessUrl ?? "https://example.local/success"}?pid={paymentId}");
        return Task.FromResult(new InitiateDonationResult("stripe", paymentId, redirect));
    }

    public async Task HandleWebhookAsync(string body, IReadOnlyDictionary<string,string> headers, CancellationToken ct)
    {
        // Placeholder webhook: accept JSON { providerPaymentId: '', status: 'succeeded|failed|canceled', raw: {} }
        // Add basic header-based signature verification stub (e.g., 'Stripe-Signature').
        try
        {
            if (string.IsNullOrWhiteSpace(body)) return;
            if (!string.IsNullOrWhiteSpace(_options.WebhookSecret))
            {
                if (!headers.TryGetValue("Stripe-Signature", out var sigHeader))
                {
                    _logger.LogWarning("Missing Stripe-Signature header");
                    return; // reject silently
                }
                // Simplified: in real implementation use Stripe SDK EventUtility.ConstructEvent
                // Here we compute HMAC SHA256 of body with secret and check contains
                using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(_options.WebhookSecret));
                var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(body));
                var expected = Convert.ToHexString(hash).ToLowerInvariant();
                if (!sigHeader.Contains(expected, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return;
                }
            }
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var pid = root.GetProperty("providerPaymentId").GetString(); // keep external JSON field name stable
            var status = root.GetProperty("status").GetString();
            var donation = await _db.Donations.FirstOrDefaultAsync(d => d.ProviderDonationId == pid, ct);
            if (donation != null)
            {
                donation.Status = status ?? donation.Status;
                donation.UpdatedUtc = DateTime.UtcNow;
                donation.ProviderDataJson = body;
                await _db.SaveChangesAsync(ct);
                _logger.LogInformation("Updated donation {DonationId} via webhook status {Status}", donation.Id, donation.Status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook handling failed");
        }
    }
}
