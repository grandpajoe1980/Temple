using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Temple.Application.Automation;
using Temple.Infrastructure.Persistence;

namespace Temple.Infrastructure.Temple.Automation;

public class DailyContentRotationJob : IDailyContentRotationJob
{
    private readonly AppDbContext _db;
    private readonly ILogger<DailyContentRotationJob> _logger;
    public DailyContentRotationJob(AppDbContext db, ILogger<DailyContentRotationJob> logger)
    { _db = db; _logger = logger; }

    public async Task RunAsync(CancellationToken ct = default)
    {
        // Placeholder: ensure at least one active DailyContent per taxonomy; could rotate 'Active' flags.
        var total = await _db.DailyContents.CountAsync(ct);
        _logger.LogInformation("DailyContentRotationJob executed. Items: {Count}", total);
    }
}
