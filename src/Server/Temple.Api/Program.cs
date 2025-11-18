using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Temple.Infrastructure.Persistence;
using Temple.Application.Tenants;
using Temple.Infrastructure.Tenants;
using Microsoft.AspNetCore.Identity;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Temple.Domain.Users;
using Temple.Api.Middleware;
using Temple.Application.Terminology;
using Temple.Infrastructure.Temple.Terminology;
using Temple.Domain.Identity;
using Temple.Domain.Scheduling;
using Temple.Domain.Audit;
using Temple.Domain.Terminology;
using Temple.Application.Donations;
using Temple.Infrastructure.Temple.Donations;
using Temple.Application.Search;
using Temple.Infrastructure.Temple.Search;
using Temple.Application.Audit;
using Temple.Infrastructure.Temple.Audit;
using StackExchange.Redis;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Temple.Application.Identity;
using Temple.Infrastructure.Temple.Identity;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.PostgreSql;
using Temple.Application.Automation;
using Temple.Infrastructure.Temple.Automation;
using Temple.Application.Scheduling;
using Temple.Infrastructure.Temple.Scheduling;
using Moq;

public record TenantUpdateRequest(string? Name, string? TaxonomyId, string? Status);
public record TenantSettingsDto(Guid Id, string Name, string Slug, string Status, string? TaxonomyId, IReadOnlyDictionary<string,string>? Terminology);

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

            // CORS for frontend (adjust origins as needed for deployment)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("frontend", policy =>
                    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true));
            });

        var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDatabase");
        builder.Services.AddDbContext<AppDbContext>(o =>
        {
            if (useInMemory)
            {
                o.UseInMemoryDatabase("test-db");
            }
            else
            {
                o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=temple;Username=postgres;Password=postgres");
            }
        });

    builder.Services.AddScoped<ITenantService, TenantService>();
    builder.Services.AddScoped<PasswordHasher<User>>();
    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddScoped<ITerminologyService, TerminologyService>();
    builder.Services.AddSignalR();
    builder.Services.AddScoped<IDonationProvider, StripeDonationProvider>();
    builder.Services.AddScoped<ISearchService, NaiveSearchService>();
    builder.Services.AddScoped<IAuditWriter, AuditWriter>();
    builder.Services.AddScoped<IDailyContentRotationJob, DailyContentRotationJob>();
    builder.Services.AddScoped<ILessonRotationJob, LessonRotationJob>();
    builder.Services.AddScoped<IEventReminderScheduler, EventReminderScheduler>();
    builder.Services.AddScoped<ICapabilityHashRegenerator, CapabilityHashRegenerator>();
    builder.Services.AddScoped<ICapabilityHashProvider, CapabilityHashProvider>();

    // Hangfire configuration & server (disabled under in-memory mode to avoid storage init errors)
    if (!useInMemory)
    {
        builder.Services.AddHangfire(config =>
        {
            var cs = builder.Configuration.GetConnectionString("Postgres") ?? "Host=localhost;Database=temple;Username=postgres;Password=postgres";
            config.UseSimpleAssemblyNameTypeSerializer()
                  .UseRecommendedSerializerSettings()
                  .UsePostgreSqlStorage(opts =>
                  {
                      opts.UseNpgsqlConnection(cs);
                      // Reasonable defaults; can tune queues, intervals later
                  }); // Configure persistent storage so JobStorage is initialized
        });
        builder.Services.AddHangfireServer();
    }
    else
    {
        // Register mock IBackgroundJobClient for in-memory mode (tests)
        builder.Services.AddSingleton<IBackgroundJobClient>(new Mock<IBackgroundJobClient>().Object);
    }

    // Redis (best-effort) for caching / future presence
    var redisConn = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    try { builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConn)); } catch { }

    builder.Services.AddRateLimiter(opts =>
    {
        opts.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var slug = context.Request.Headers["X-Tenant-Slug"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(slug))
            {
                var host = context.Request.Host.Host;
                var parts = host.Split('.');
                if (parts.Length > 2) slug = parts[0];
            }
            slug ??= context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return System.Threading.RateLimiting.RateLimitPartition.GetTokenBucketLimiter(slug, _ => new System.Threading.RateLimiting.TokenBucketRateLimiterOptions
            {
                TokenLimit = 300,
                TokensPerPeriod = 150,
                ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                AutoReplenishment = true
            });
        });

        opts.AddPolicy("auth-login", context => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            (context.Request.Headers["X-Tenant-Slug"].FirstOrDefault() ?? "no-tenant") + ":" + (context.Connection.RemoteIpAddress?.ToString() ?? "noip"), _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            }));

        opts.AddPolicy("donation-initiate", context => System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            (context.Request.Headers["X-Tenant-Slug"].FirstOrDefault() ?? "no-tenant") + ":donate:" + (context.Connection.RemoteIpAddress?.ToString() ?? "noip"), _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(5),
                SegmentsPerWindow = 5,
                QueueLimit = 0,
                QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst
            }));
    });

    builder.Services.AddOptions<JwtOptions>().BindConfiguration("Jwt");

    builder.Services.AddSingleton<JwtSecurityTokenHandler>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var jwtOpts = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOpts.Issuer),
                ValidateAudience = !string.IsNullOrWhiteSpace(jwtOpts.Audience),
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOpts.Issuer,
                ValidAudience = jwtOpts.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Secret ?? "dev-secret-change"))
            };
        });

    // Basic password policy (manual since not using full IdentityUser yet)
    builder.Services.Configure<PasswordHasherOptions>(o =>
    {
        o.IterationCount = 15_000; // stronger hashing cost (PBKDF2 default ~10k)
    });

    builder.Services.AddAuthorization(options =>
    {
        foreach (var cap in Capability.All)
        {
            options.AddPolicy(cap, policy => policy.RequireAssertion(ctx =>
            {
                return ctx.User.HasClaim("cap", cap) || ctx.User.IsInRole("super_admin") || ctx.User.HasClaim("super", "1");
            }));
        }
    });

    builder.Services.AddHostedService<SeedStartupData>();

        builder.Services.AddHealthChecks();

        var app = builder.Build();

        // Ensure database is initialized before running hosted services that depend on schema
        using (var scope = app.Services.CreateScope())
        {
            try
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                if (useInMemory)
                    db.Database.EnsureCreated();
                else
                    db.Database.Migrate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database migration failed: {ex.Message}");
                throw; // rethrow so failure is visible
            }
        }

    if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // Middleware pipeline (partial per architecture doc)
        app.UseCorrelationId();
        app.UseGlobalExceptionHandling();
        app.UseSecurityHeaders();
    app.UseRateLimiter();
        app.UseCors("frontend");
        app.UseTenantResolution();
    app.UseTerminology();
        app.UseAuthentication();
        app.UseCapabilityHashValidation();
        app.UseAuthorization();

    // Friendly root redirect for UAT testers
    app.MapGet("/", () => Results.Redirect("/swagger"));

        app.MapHealthChecks("/health");

        // Versioned API group
        var api = app.MapGroup("/api/v1");

        // Chat history (basic pagination per channel)
        api.MapGet("/chat/{channelKey}/messages", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatPostMessage)] async (
            string channelKey,
            int page, int pageSize,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 50 : pageSize;
            var channel = await db.ChatChannels.FirstOrDefaultAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == channelKey.ToLower(), ct);
            if (channel == null) return Results.NotFound();
            var query = db.ChatMessages.Where(m => m.TenantId == tenantCtx.TenantId && m.ChannelId == channel.Id);
            var total = await query.CountAsync(ct);
            var data = await query.OrderByDescending(m => m.CreatedUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data = data.Select(m => new { m.Id, m.Body, m.UserId, m.CreatedUtc, m.Type }), page, pageSize, total });
        });

        // Chat post message (aligns with planned POST /api/chat/channels/{id}/messages)
        api.MapPost("/chat/{channelKey}/messages", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatPostMessage)] async (
            string channelKey,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            [FromBody] ChatMessageCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var channel = await db.ChatChannels.FirstOrDefaultAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == channelKey.ToLower(), ct);
            if (channel == null) return Results.NotFound();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            var message = new Temple.Domain.Chat.ChatMessage { TenantId = tenantCtx.TenantId.Value, ChannelId = channel.Id, Body = req.Body ?? string.Empty, UserId = userId };
            db.ChatMessages.Add(message);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/chat/{channelKey}/messages/{message.Id}", new { message.Id, message.Body, message.CreatedUtc });
        });

        // Chat channels list (temporary auth reuse of ChatPostMessage until chat.read capability added)
        api.MapGet("/chat/channels", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatPostMessage)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.ChatChannels.Where(c => c.TenantId == tenantCtx.TenantId)
                .OrderBy(c => c.IsSystem ? 0 : 1).ThenBy(c => c.Name)
                .Select(c => new { c.Key, c.Name, c.IsSystem })
                .ToListAsync(ct);
            return Results.Ok(list);
        });

        // Chat channel create
        api.MapPost("/chat/channels", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatCreateChannel)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            [FromBody] ChatChannelCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest();
            var key = req.Key.Trim().ToLowerInvariant();
            if (!key.All(c => char.IsLetterOrDigit(c) || c=='-' || c=='_')) return Results.BadRequest("Invalid key");
            var exists = await db.ChatChannels.AnyAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == key, ct);
            if (exists) return Results.Conflict();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            Guid? userId = Guid.TryParse(userIdStr, out var u) ? u : null;
            var channel = new Temple.Domain.Chat.ChatChannel { TenantId = tenantCtx.TenantId.Value, Key = key, Name = req.Name.Trim(), Description = req.Description, CreatedByUserId = userId, IsPrivate = req.IsPrivate };
            db.ChatChannels.Add(channel);
            if (userId != null && channel.IsPrivate)
            {
                db.ChatChannelMembers.Add(new Temple.Domain.Chat.ChatChannelMember { TenantId = tenantCtx.TenantId.Value, ChannelId = channel.Id, UserId = userId.Value, IsModerator = true });
            }
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/chat/channels/{channel.Key}", new { channel.Key, channel.Name, channel.IsPrivate });
        });

        // Chat channel delete
        api.MapDelete("/chat/channels/{key}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatDeleteChannel)] async (
            string key,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            key = key.ToLowerInvariant();
            var channel = await db.ChatChannels.FirstOrDefaultAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == key, ct);
            if (channel == null) return Results.NotFound();
            if (channel.IsSystem) return Results.BadRequest("Cannot delete system channel");
            db.ChatChannels.Remove(channel);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // Chat channel join
        api.MapPost("/chat/channels/{key}/join", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatPostMessage)] async (
            string key,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            key = key.ToLowerInvariant();
            var channel = await db.ChatChannels.FirstOrDefaultAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == key, ct);
            if (channel == null) return Results.NotFound();
            if (channel.IsPrivate)
            {
                // For now allow self-join if not private membership enforcement yet
            }
            var exists = await db.ChatChannelMembers.FirstOrDefaultAsync(m => m.TenantId == tenantCtx.TenantId && m.ChannelId == channel.Id && m.UserId == userId, ct);
            if (exists == null)
            {
                db.ChatChannelMembers.Add(new Temple.Domain.Chat.ChatChannelMember { TenantId = tenantCtx.TenantId.Value, ChannelId = channel.Id, UserId = userId });
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok();
        });

        // Chat channel leave
        api.MapPost("/chat/channels/{key}/leave", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatPostMessage)] async (
            string key,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            key = key.ToLowerInvariant();
            var channel = await db.ChatChannels.FirstOrDefaultAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == key, ct);
            if (channel == null) return Results.NotFound();
            var member = await db.ChatChannelMembers.FirstOrDefaultAsync(m => m.TenantId == tenantCtx.TenantId && m.ChannelId == channel.Id && m.UserId == userId, ct);
            if (member != null)
            {
                db.ChatChannelMembers.Remove(member);
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok();
        });

        // Presence list (currently active users) - naive implementation
        api.MapGet("/chat/presence", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ChatReadPresence)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var users = await db.ChatPresences.Where(p => p.TenantId == tenantCtx.TenantId && p.DisconnectedUtc == null && p.LastActiveUtc >= cutoff)
                .GroupBy(p => p.UserId)
                .Select(g => new { userId = g.Key, lastActiveUtc = g.Max(x => x.LastActiveUtc) })
                .ToListAsync(ct);
            return Results.Ok(users);
        });

    // Donation create (direct record; immediate success simulation)
        api.MapPost("/donations", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationCreate)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            [FromBody] DonationCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            Guid? userId = Guid.TryParse(userIdStr, out var g) ? g : null;
            // Validate optional target goal/fund
            Temple.Domain.Finance.FinanceGoal? goal = null;
            Temple.Domain.Stewardship.StewardshipFund? fund = null;
            if (req.FinanceGoalId.HasValue)
            {
                goal = await db.FinanceGoals.FirstOrDefaultAsync(f => f.Id == req.FinanceGoalId && f.TenantId == tenantCtx.TenantId, ct);
                if (goal == null) return Results.BadRequest(new { message = "GOAL_NOT_FOUND" });
            }
            if (req.StewardshipFundId.HasValue)
            {
                fund = await db.StewardshipFunds.FirstOrDefaultAsync(f => f.Id == req.StewardshipFundId && f.TenantId == tenantCtx.TenantId, ct);
                if (fund == null) return Results.BadRequest(new { message = "FUND_NOT_FOUND" });
            }
            var donation = new Temple.Domain.Donations.Donation { TenantId = tenantCtx.TenantId.Value, UserId = userId, AmountCents = req.AmountCents, Currency = req.Currency ?? "usd", Recurring = req.Recurring, FinanceGoalId = req.FinanceGoalId, StewardshipFundId = req.StewardshipFundId, Status = "succeeded" };
            db.Donations.Add(donation);
            // Update aggregates
            if (goal != null)
            {
                goal.CurrentAmount += donation.AmountCents / 100m; // goal uses decimal currency
            }
            if (fund != null)
            {
                fund.Balance += donation.AmountCents / 100m;
                // also ledger entry for traceability
                db.StewardshipFundLedgerEntries.Add(new Temple.Domain.Stewardship.StewardshipFundLedgerEntry { TenantId = tenantCtx.TenantId.Value, FundId = fund.Id, CampaignId = null, DonationId = donation.Id, Amount = donation.AmountCents / 100m, Type = "donation", Notes = "Direct designated donation" });
            }
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/donations/{donation.Id}", donation);
        });

        // Planned aligned endpoint: initiate donation via provider abstraction
    api.MapPost("/donations/initiate", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationCreate)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            [FromBody] DonationCreateRequest req,
            IDonationProvider provider,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            Guid? userId = Guid.TryParse(userIdStr, out var g) ? g : null;
            // Validate targets
            Temple.Domain.Finance.FinanceGoal? goal = null;
            Temple.Domain.Stewardship.StewardshipFund? fund = null;
            if (req.FinanceGoalId.HasValue)
            {
                goal = await db.FinanceGoals.FirstOrDefaultAsync(f => f.Id == req.FinanceGoalId && f.TenantId == tenantCtx.TenantId, ct);
                if (goal == null) return Results.BadRequest(new { message = "GOAL_NOT_FOUND" });
            }
            if (req.StewardshipFundId.HasValue)
            {
                fund = await db.StewardshipFunds.FirstOrDefaultAsync(f => f.Id == req.StewardshipFundId && f.TenantId == tenantCtx.TenantId, ct);
                if (fund == null) return Results.BadRequest(new { message = "FUND_NOT_FOUND" });
            }
            var result = await provider.InitiateAsync(new DonationInitiateRequest(tenantCtx.TenantId.Value, userId, req.AmountCents, req.Currency ?? "usd", req.Recurring), ct);
            var donation = new Temple.Domain.Donations.Donation { TenantId = tenantCtx.TenantId.Value, UserId = userId, AmountCents = req.AmountCents, Currency = req.Currency ?? "usd", Recurring = req.Recurring, Provider = result.Provider, ProviderDonationId = result.ProviderPaymentId, FinanceGoalId = req.FinanceGoalId, StewardshipFundId = req.StewardshipFundId };
            db.Donations.Add(donation);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/donations/{donation.Id}", new { donation.Id, donation.AmountCents, donation.Currency, donation.Recurring, result.RedirectUrl });
    }).RequireRateLimiting("donation-initiate");

        api.MapGet("/donations", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationViewSummary)] async (TenantContext tenantCtx, AppDbContext db, int page, int pageSize, CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 50 : pageSize;
            var query = db.Donations.Where(d => d.TenantId == tenantCtx.TenantId);
            var total = await query.CountAsync(ct);
            var data = await query.OrderByDescending(d => d.CreatedUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            var sum = await query.SumAsync(d => d.AmountCents, ct);
            return Results.Ok(new { data, page, pageSize, total, sumCents = sum });
        });

        // Donation summary (separate endpoint aligning with docs)
        api.MapGet("/donations/summary", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationViewSummary)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var query = db.Donations.Where(d => d.TenantId == tenantCtx.TenantId);
            var total = await query.CountAsync(ct);
            var sum = await query.SumAsync(d => d.AmountCents, ct);
            var recurring = await query.CountAsync(d => d.Recurring, ct);
            return Results.Ok(new { total, sumCents = sum, recurringCount = recurring });
        });

        // Recurring commitments (tithes / pledges) - create
        api.MapPost("/donations/recurring", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationCreate)] async (
            TenantContext tenantCtx,
            ClaimsPrincipal user,
            AppDbContext db,
            [FromBody] RecurringCommitmentCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : null;
            if (req.AmountCents <= 0) return Results.BadRequest(new { message = "Amount must be positive" });
            Temple.Domain.Finance.FinanceGoal? goal = null;
            Temple.Domain.Stewardship.StewardshipFund? fund = null;
            if (req.FinanceGoalId.HasValue)
            {
                goal = await db.FinanceGoals.FirstOrDefaultAsync(g => g.Id == req.FinanceGoalId && g.TenantId == tenantCtx.TenantId, ct);
                if (goal == null) return Results.BadRequest(new { message = "GOAL_NOT_FOUND" });
            }
            if (req.StewardshipFundId.HasValue)
            {
                fund = await db.StewardshipFunds.FirstOrDefaultAsync(f => f.Id == req.StewardshipFundId && f.TenantId == tenantCtx.TenantId, ct);
                if (fund == null) return Results.BadRequest(new { message = "FUND_NOT_FOUND" });
            }
            var rc = new Temple.Domain.Finance.RecurringCommitment { TenantId = tenantCtx.TenantId.Value, UserId = userId, AmountCents = req.AmountCents, Frequency = req.Frequency ?? "monthly", FinanceGoalId = req.FinanceGoalId, StewardshipFundId = req.StewardshipFundId, Notes = req.Notes };
            db.RecurringCommitments.Add(rc);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/donations/recurring/{rc.Id}", rc);
        });

        api.MapGet("/donations/recurring", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationViewSummary)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.RecurringCommitments.Where(r => r.TenantId == tenantCtx.TenantId && r.Active).OrderBy(r => r.CreatedUtc).ToListAsync(ct);
            return Results.Ok(list);
        });

        api.MapPost("/donations/recurring/{id:guid}/deactivate", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationCreate)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var rc = await db.RecurringCommitments.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantCtx.TenantId, ct);
            if (rc == null) return Results.NotFound();
            rc.Active = false;
            rc.EndUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(rc);
        });

        // Finance Goals (basic create & list)
        api.MapPost("/finance/goals", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceManageBudget)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] FinanceGoalCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Key and Name required" });
            var exists = await db.FinanceGoals.AnyAsync(g => g.TenantId == tenantCtx.TenantId && g.Key == req.Key, ct);
            if (exists) return Results.Conflict(new { message = "Key already exists" });
            var goal = new Temple.Domain.Finance.FinanceGoal { TenantId = tenantCtx.TenantId.Value, Key = req.Key.Trim().ToLowerInvariant(), Name = req.Name.Trim(), Description = req.Description, TargetAmount = req.TargetAmount ?? 0m, CurrentAmount = 0m, StartUtc = req.StartUtc?.ToUniversalTime() ?? DateTime.UtcNow, EndUtc = req.EndUtc?.ToUniversalTime() };
            db.FinanceGoals.Add(goal);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/finance/goals/{goal.Id}", goal);
        });

        api.MapGet("/finance/goals", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceManageBudget)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var goals = await db.FinanceGoals.Where(g => g.TenantId == tenantCtx.TenantId && !g.IsArchived).OrderBy(g => g.StartUtc).ToListAsync(ct);
            return Results.Ok(goals);
        });

        // Finance Dashboard (encouraging summary)
    api.MapGet("/finance/dashboard", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationViewSummary)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            int? lookbackDays,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var days = lookbackDays is >0 and <=365 ? lookbackDays.Value : 30;
            var since = DateTime.UtcNow.AddDays(-days);
            var donations = db.Donations.Where(d => d.TenantId == tenantCtx.TenantId && d.Status == "succeeded");
            var periodDonations = donations.Where(d => d.CreatedUtc >= since);
            var periodTotalCents = await periodDonations.SumAsync(d => d.AmountCents, ct);
            var lifetimeTotalCents = await donations.SumAsync(d => d.AmountCents, ct);
            var activeGoals = await db.FinanceGoals.Where(g => g.TenantId == tenantCtx.TenantId && !g.IsArchived && (g.EndUtc == null || g.EndUtc >= DateTime.UtcNow)).ToListAsync(ct);
            var funds = await db.StewardshipFunds.Where(f => f.TenantId == tenantCtx.TenantId && !f.IsArchived).OrderBy(f => f.Name).ToListAsync(ct);
            var encouragement = FinanceDashboardHelper.GenerateEncouragementMessage(lifetimeTotalCents, periodTotalCents, activeGoals, funds);
            var goalSummaries = activeGoals.Select(g => new {
                g.Id, g.Key, g.Name, g.TargetAmount, g.CurrentAmount,
                progress = g.TargetAmount == 0 ? 0 : Math.Round((double)(g.CurrentAmount / g.TargetAmount) * 100, 1)
            });
            var fundSummaries = funds.Select(f => new { f.Id, f.Name, f.Code, f.Balance });
            return Results.Ok(new {
                lookbackDays = days,
                periodTotalCents,
                lifetimeTotalCents,
                goals = goalSummaries,
                funds = fundSummaries,
                encouragement
            });
        });

        // Finance: Budget Categories
        api.MapPost("/finance/budgets", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceManageBudget)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] BudgetCategoryCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Key and Name required" });
            var exists = await db.BudgetCategories.AnyAsync(c => c.TenantId == tenantCtx.TenantId && c.Key == req.Key, ct);
            if (exists) return Results.Conflict(new { message = "Key exists" });
            var cat = new Temple.Domain.Finance.BudgetCategory { TenantId = tenantCtx.TenantId.Value, Key = req.Key.Trim().ToLowerInvariant(), Name = req.Name.Trim(), PeriodKey = req.PeriodKey, BudgetAmountCents = req.BudgetAmountCents ?? 0 };
            db.BudgetCategories.Add(cat);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/finance/budgets/{cat.Id}", cat);
        });

        api.MapGet("/finance/budgets", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceManageBudget)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            string? periodKey,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var q = db.BudgetCategories.Where(c => c.TenantId == tenantCtx.TenantId && !c.IsArchived);
            if (!string.IsNullOrWhiteSpace(periodKey)) q = q.Where(c => c.PeriodKey == periodKey);
            var list = await q.OrderBy(c => c.PeriodKey).ThenBy(c => c.Name).ToListAsync(ct);
            return Results.Ok(list);
        });

        // Finance: Expense submission
        api.MapPost("/finance/expenses", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceSubmitExpense)] async (
            TenantContext tenantCtx,
            ClaimsPrincipal user,
            AppDbContext db,
            [FromBody] ExpenseSubmitRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            Guid? userId = Guid.TryParse(userIdStr, out var uid) ? uid : null;
            var cat = await db.BudgetCategories.FirstOrDefaultAsync(c => c.Id == req.BudgetCategoryId && c.TenantId == tenantCtx.TenantId, ct);
            if (cat == null) return Results.BadRequest(new { message = "CATEGORY_NOT_FOUND" });
            if (req.AmountCents <= 0) return Results.BadRequest(new { message = "Amount must be positive" });
            var expense = new Temple.Domain.Finance.Expense { TenantId = tenantCtx.TenantId.Value, BudgetCategoryId = cat.Id, AmountCents = req.AmountCents, Description = req.Description ?? string.Empty, SubmittedByUserId = userId };
            db.Expenses.Add(expense);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/finance/expenses/{expense.Id}", expense);
        });

        api.MapGet("/finance/expenses", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceManageBudget)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            string? status,
            Guid? categoryId,
            int page,
            int pageSize,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 50 : pageSize;
            var q = db.Expenses.Where(e => e.TenantId == tenantCtx.TenantId);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(e => e.Status == status);
            if (categoryId.HasValue) q = q.Where(e => e.BudgetCategoryId == categoryId.Value);
            var total = await q.CountAsync(ct);
            var data = await q.OrderByDescending(e => e.SubmittedUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        // Expense approve / reject
        api.MapPost("/finance/expenses/{id:guid}/approve", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceApproveExpense)] async (
            Guid id,
            TenantContext tenantCtx,
            ClaimsPrincipal user,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var exp = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantCtx.TenantId, ct);
            if (exp == null) return Results.NotFound();
            if (exp.Status != "submitted") return Results.BadRequest(new { message = "INVALID_STATE" });
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var approverId)) return Results.Unauthorized();
            exp.Status = "approved";
            exp.ApprovedByUserId = approverId;
            exp.ApprovedUtc = DateTime.UtcNow;
            // update budget actuals
            var cat = await db.BudgetCategories.FirstAsync(c => c.Id == exp.BudgetCategoryId, ct);
            cat.ActualAmountCents += exp.AmountCents;
            await db.SaveChangesAsync(ct);
            return Results.Ok(exp);
        });

        api.MapPost("/finance/expenses/{id:guid}/reject", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.FinanceApproveExpense)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] ExpenseRejectRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var exp = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantCtx.TenantId, ct);
            if (exp == null) return Results.NotFound();
            if (exp.Status != "submitted") return Results.BadRequest(new { message = "INVALID_STATE" });
            exp.Status = "rejected";
            exp.Notes = req.Reason;
            await db.SaveChangesAsync(ct);
            return Results.Ok(exp);
        });

        // Donation leaderboard (top donors by total amount) period options: all,30d,7d
        api.MapGet("/donations/leaderboard", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.DonationViewSummary)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            string? period,
            int? limit,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            period = string.IsNullOrWhiteSpace(period) ? "all" : period.ToLowerInvariant();
            var l = Math.Clamp(limit ?? 10, 1, 100);
            var since = period switch
            {
                "30d" => DateTime.UtcNow.AddDays(-30),
                "7d" => DateTime.UtcNow.AddDays(-7),
                _ => (DateTime?)null
            };
            var donations = db.Donations.Where(d => d.TenantId == tenantCtx.TenantId && d.UserId != null && (d.Status == "succeeded" || d.Status == "pending"));
            if (since != null) donations = donations.Where(d => d.CreatedUtc >= since);
            var agg = await donations
                .GroupBy(d => d.UserId!)
                .Select(g => new { UserId = g.Key, totalAmountCents = g.Sum(x => x.AmountCents), count = g.Count(), lastDonationUtc = g.Max(x => x.CreatedUtc) })
                .OrderByDescending(x => x.totalAmountCents)
                .ThenBy(x => x.UserId)
                .Take(l)
                .ToListAsync(ct);
            // join user display names
            var userIds = agg.Select(a => a.UserId).ToList();
            var users = await db.Users.Where(u => userIds.Contains(u.Id)).Select(u => new { u.Id, u.DisplayName, u.Email }).ToListAsync(ct);
            var items = agg.Select(a => new {
                a.UserId,
                name = users.FirstOrDefault(u => u.Id == a.UserId)?.DisplayName ?? users.FirstOrDefault(u => u.Id == a.UserId)?.Email ?? a.UserId.ToString(),
                a.totalAmountCents,
                a.count,
                a.lastDonationUtc
            });
            return Results.Ok(new { period, items });
        });

        // Stripe webhook (unauthenticated; rely on future signature validation)
        api.MapPost("/donations/webhook/stripe", async (
            HttpContext http,
            IDonationProvider provider,
            CancellationToken ct) =>
        {
            using var reader = new StreamReader(http.Request.Body);
            var body = await reader.ReadToEndAsync(ct);
            var headers = http.Request.Headers.ToDictionary(k => k.Key, v => v.Value.ToString());
            await provider.HandleWebhookAsync(body, headers, ct);
            return Results.Ok();
        });

        // Profile update
        api.MapPost("/profile", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ProfileUpdate)] async (AppDbContext db, ClaimsPrincipal user, [FromBody] ProfileUpdateRequest req, CancellationToken ct) =>
        {
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            var entity = await db.Users.FindAsync(new object?[] { userId }, ct);
            if (entity == null) return Results.NotFound();
            entity.DisplayName = string.IsNullOrWhiteSpace(req.DisplayName) ? entity.DisplayName : req.DisplayName;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { entity.Id, entity.DisplayName });
        });

        // Custom role label set
        api.MapPost("/roles/{userId:guid}/label", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            Guid userId,
            [FromBody] RoleLabelRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var tu = await db.TenantUsers.FirstOrDefaultAsync(t => t.TenantId == tenantCtx.TenantId && t.UserId == userId, ct);
            if (tu == null) return Results.NotFound();
            tu.CustomRoleLabel = req.Label;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { tu.UserId, tu.RoleKey, tu.CustomRoleLabel });
        });

        // List roles for tenant (including custom labels for each user)
        api.MapGet("/roles/users", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (TenantContext tenantCtx, AppDbContext db, CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.TenantUsers.Where(t => t.TenantId == tenantCtx.TenantId)
                .Select(t => new { t.UserId, t.RoleKey, t.CustomRoleLabel })
                .ToListAsync(ct);
            return Results.Ok(list);
        });

        // Notifications - create (queued if not in-app) & immediate mark sent for in-app
        api.MapPost("/notifications", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.NotificationSend)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] NotificationSendRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var channel = string.IsNullOrWhiteSpace(req.Channel) ? "inapp" : req.Channel!.Trim().ToLowerInvariant();
            var notif = new Temple.Domain.Notifications.Notification {
                TenantId = tenantCtx.TenantId.Value,
                UserId = req.UserId,
                Channel = channel,
                Subject = req.Subject ?? string.Empty,
                Body = req.Body ?? string.Empty,
                Status = channel == "inapp" ? Temple.Domain.Notifications.NotificationDeliveryStatus.Sent : Temple.Domain.Notifications.NotificationDeliveryStatus.Pending,
                SentUtc = channel == "inapp" ? DateTime.UtcNow : null
            };
            db.Notifications.Add(notif);
            await db.SaveChangesAsync(ct);
            // Future: enqueue background job for non-inapp channels
            return Results.Created($"/api/v1/notifications/{notif.Id}", new { notif.Id, notif.Channel, notif.Status, notif.CreatedUtc });
        });

        // Notifications - list current user's in-app (simple pagination)
        api.MapGet("/notifications", [Microsoft.AspNetCore.Authorization.Authorize] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            int page, int pageSize,
            string? channel,
            string? status,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 20 : pageSize;
            var q = db.Notifications.Where(n => n.TenantId == tenantCtx.TenantId && (n.UserId == null || n.UserId == userId));
            if (!string.IsNullOrWhiteSpace(channel)) q = q.Where(n => n.Channel == channel);
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<Temple.Domain.Notifications.NotificationDeliveryStatus>(status, true, out var st)) q = q.Where(n => n.Status == st);
            var total = await q.CountAsync(ct);
            var data = await q.OrderByDescending(n => n.CreatedUtc).Skip((page-1)*pageSize).Take(pageSize)
                .Select(n => new { n.Id, n.Channel, n.Subject, n.CreatedUtc, n.SentUtc, n.Status, n.ReadUtc })
                .ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        // Notifications - mark read
    api.MapPost("/notifications/{id:guid}/read", [Microsoft.AspNetCore.Authorization.Authorize] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            var notif = await db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.TenantId == tenantCtx.TenantId, ct);
            if (notif == null) return Results.NotFound();
            if (notif.UserId != null && notif.UserId != userId) return Results.Forbid();
            if (notif.UserId == null)
            {
                // broadcast; track in user state
                var existing = await db.NotificationUserStates.FirstOrDefaultAsync(s => s.NotificationId == notif.Id && s.UserId == userId, ct);
                if (existing == null)
                {
                    db.NotificationUserStates.Add(new Temple.Domain.Notifications.NotificationUserState { TenantId = tenantCtx.TenantId.Value, NotificationId = notif.Id, UserId = userId, ReadUtc = DateTime.UtcNow });
                }
                else if (existing.ReadUtc == null)
                {
                    existing.ReadUtc = DateTime.UtcNow;
                }
            }
            else
            {
                if (notif.ReadUtc == null) notif.ReadUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });

        // Notification preferences - upsert
    api.MapPost("/notifications/preferences", [Microsoft.AspNetCore.Authorization.Authorize] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            [FromBody] NotificationPreferenceRequest pref,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            var channel = pref.Channel?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(channel)) return Results.BadRequest();
            var existing = await db.NotificationPreferences.FirstOrDefaultAsync(p => p.TenantId == tenantCtx.TenantId && p.UserId == userId && p.Channel == channel, ct);
            if (existing == null)
            {
                existing = new Temple.Domain.Notifications.NotificationPreference { TenantId = tenantCtx.TenantId.Value, UserId = userId, Channel = channel, Enabled = pref.Enabled };
                db.NotificationPreferences.Add(existing);
            }
            else
            {
                existing.Enabled = pref.Enabled;
                existing.UpdatedUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { existing.Channel, existing.Enabled });
        });

        // Notification preferences - list for current user
    api.MapGet("/notifications/preferences", [Microsoft.AspNetCore.Authorization.Authorize] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var userIdStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(userIdStr, out var userId)) return Results.Unauthorized();
            var prefs = await db.NotificationPreferences.Where(p => p.TenantId == tenantCtx.TenantId && p.UserId == userId)
                .Select(p => new { p.Channel, p.Enabled }).ToListAsync(ct);
            return Results.Ok(prefs);
        });

        // Search (abstracted service for future Postgres FTS implementation)
        api.MapGet("/search", async (TenantContext tenantCtx, ISearchService search, string q, int limit, CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            q = q?.Trim() ?? string.Empty;
            var items = await search.SearchAsync(tenantCtx.TenantId.Value, q, limit, ct);
            return Results.Ok(new { items, total = items.Count });
        });

        // Lessons (content)
        api.MapPost("/content/lessons", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ContentCreateLesson)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            [FromBody] LessonCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var lesson = new Temple.Domain.Content.Lesson { TenantId = tenantCtx.TenantId.Value, Title = req.Title, Body = req.Body ?? string.Empty, Tags = req.Tags ?? Array.Empty<string>() };
            db.Lessons.Add(lesson);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/content/lessons/{lesson.Id}", lesson);
        });
        api.MapGet("/content/lessons", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            int page, int pageSize,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 20 : pageSize;
            var query = db.Lessons.Where(l => l.TenantId == tenantCtx.TenantId);
            var total = await query.CountAsync(ct);
            var data = await query.OrderByDescending(l => l.CreatedUtc).Skip((page-1)*pageSize).Take(pageSize).Select(l => new { l.Id, l.Title, l.PublishedUtc, l.CreatedUtc }).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });
        api.MapPost("/content/lessons/{id:guid}/publish", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ContentPublishLesson)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantCtx.TenantId, ct);
            if (lesson == null) return Results.NotFound();
            lesson.PublishedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { lesson.Id, lesson.PublishedUtc });
        });

        // Lesson update
        api.MapPut("/content/lessons/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ContentCreateLesson)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] LessonUpdateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantCtx.TenantId, ct);
            if (lesson == null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(req.Title)) lesson.Title = req.Title.Trim();
            if (req.Body != null) lesson.Body = req.Body;
            if (req.Tags != null) lesson.Tags = req.Tags;
            lesson.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { lesson.Id });
        });

        // Lesson delete
        api.MapDelete("/content/lessons/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ContentPublishLesson)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantCtx.TenantId, ct);
            if (lesson == null) return Results.NotFound();
            db.Lessons.Remove(lesson);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // Media asset create (upload stub)
        api.MapPost("/media/assets", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.MediaUpload)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] MediaAssetCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var asset = new Temple.Domain.Content.MediaAsset { TenantId = tenantCtx.TenantId.Value, Title = req.Title, Type = req.Type ?? "audio", StorageKey = req.StorageKey ?? Guid.NewGuid().ToString("N"), Status = "uploaded" };
            db.MediaAssets.Add(asset);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/media/assets/{asset.Id}", asset);
        });

        api.MapGet("/media/assets", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            int page, int pageSize,
            string? type,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 20 : pageSize;
            var query = db.MediaAssets.Where(m => m.TenantId == tenantCtx.TenantId);
            if (!string.IsNullOrWhiteSpace(type)) query = query.Where(m => m.Type == type);
            var total = await query.CountAsync(ct);
            var data = await query.OrderByDescending(m => m.CreatedUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        // Generate upload URL (stub) - would normally call storage service for pre-signed URL
        api.MapPost("/media/assets/upload-url", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.MediaUpload)] (
            TenantContext tenantCtx,
            [FromBody] MediaUploadUrlRequest req) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var key = $"{tenantCtx.TenantId}/{Guid.NewGuid():N}{(string.IsNullOrWhiteSpace(req.Extension) ? "" : req.Extension.StartsWith('.') ? req.Extension : "." + req.Extension)}";
            return Results.Ok(new { storageKey = key, uploadUrl = $"https://storage.local/{key}", method = "PUT", headers = new { } });
        });

        // Attach media to lesson
        api.MapPost("/content/lessons/{lessonId:guid}/media", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ContentCreateLesson)] async (
            Guid lessonId,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] LessonAttachMediaRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && l.TenantId == tenantCtx.TenantId, ct);
            if (lesson == null) return Results.NotFound();
            var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == req.MediaAssetId && m.TenantId == tenantCtx.TenantId, ct);
            if (asset == null) return Results.BadRequest("Media not found");
            var existingCount = await db.LessonMedia.CountAsync(l => l.LessonId == lessonId && l.TenantId == tenantCtx.TenantId, ct);
            var link = new Temple.Domain.Content.LessonMedia { TenantId = tenantCtx.TenantId.Value, LessonId = lessonId, MediaAssetId = asset.Id, SortOrder = existingCount };
            db.LessonMedia.Add(link);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/content/lessons/{lessonId}/media/{link.Id}", new { link.Id });
        });

        // List media for lesson
        api.MapGet("/content/lessons/{lessonId:guid}/media", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleRead)] async (
            Guid lessonId,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var data = await db.LessonMedia.Where(l => l.TenantId == tenantCtx.TenantId && l.LessonId == lessonId)
                .Join(db.MediaAssets, lm => lm.MediaAssetId, ma => ma.Id, (lm, ma) => new { lm.Id, mediaId = ma.Id, ma.Title, ma.Type, ma.StorageKey, lm.SortOrder })
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);
            return Results.Ok(data);
        });

        // Attach media to schedule event (sermon recording etc.)
        api.MapPost("/schedule/events/{eventId:guid}/media", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleManageEvent)] async (
            Guid eventId,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] EventAttachMediaRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var ev = await db.ScheduleEvents.FirstOrDefaultAsync(e => e.Id == eventId && e.TenantId == tenantCtx.TenantId, ct);
            if (ev == null) return Results.NotFound();
            var asset = await db.MediaAssets.FirstOrDefaultAsync(m => m.Id == req.MediaAssetId && m.TenantId == tenantCtx.TenantId, ct);
            if (asset == null) return Results.BadRequest("Media not found");
            var existingCount = await db.EventMedia.CountAsync(e => e.ScheduleEventId == eventId && e.TenantId == tenantCtx.TenantId, ct);
            var link = new Temple.Domain.Content.EventMedia { TenantId = tenantCtx.TenantId.Value, ScheduleEventId = eventId, MediaAssetId = asset.Id, SortOrder = existingCount };
            db.EventMedia.Add(link);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/schedule/events/{eventId}/media/{link.Id}", new { link.Id });
        });

        // List media for event
        api.MapGet("/schedule/events/{eventId:guid}/media", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleRead)] async (
            Guid eventId,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var data = await db.EventMedia.Where(e => e.TenantId == tenantCtx.TenantId && e.ScheduleEventId == eventId)
                .Join(db.MediaAssets, em => em.MediaAssetId, ma => ma.Id, (em, ma) => new { em.Id, mediaId = ma.Id, ma.Title, ma.Type, ma.StorageKey, em.SortOrder })
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);
            return Results.Ok(data);
        });

        api.MapGet("/tenants", async (AppDbContext db, CancellationToken ct) =>
            await db.Tenants.OrderBy(t => t.CreatedUtc).ToListAsync(ct));
        api.MapGet("/tenants/by-slug/{slug}", async ([FromRoute] string slug, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(slug)) return Results.BadRequest();
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);
            return tenant is null ? Results.NotFound() : Results.Ok(tenant);
        });
        // Aligned route per ROUTING.md planned naming
        api.MapGet("/tenants/slug/{slug}", async ([FromRoute] string slug, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(slug)) return Results.BadRequest();
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug, ct);
            return tenant is null ? Results.NotFound() : Results.Ok(tenant);
        });
        api.MapGet("/tenants/{id:guid}", async ([FromRoute] Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FindAsync(new object?[] { id }, ct);
            return tenant is null ? Results.NotFound() : Results.Ok(tenant);
        });
        api.MapPost("/tenants", async ([FromBody] TenantCreateRequest req, ITenantService service, CancellationToken ct) =>
        {
            var created = await service.CreateAsync(req, ct);
            return Results.Created($"/api/v1/tenants/{created.Id}", created);
        });

        // List top-level religions with basic info
        api.MapGet("/taxonomies/religions", async (AppDbContext db, CancellationToken ct) =>
        {
            var religions = await db.ReligionTaxonomies.Where(t => t.Type == "religion")
                .Select(t => new { t.Id, t.DisplayName })
                .OrderBy(t => t.DisplayName)
                .ToListAsync(ct);
            return Results.Ok(religions);
        });

        // List sects for a given religion id
        api.MapGet("/taxonomies/religions/{id}/sects", async (string id, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest();
            var sects = await db.ReligionTaxonomies.Where(t => t.ParentId == id && t.Type == "sect")
                .Select(t => new { t.Id, t.DisplayName, t.CanonicalTexts })
                .OrderBy(t => t.DisplayName)
                .ToListAsync(ct);
            return Results.Ok(sects);
        });

        // Get canonical texts & terminology defaults for taxonomy (religion or sect). If sect, include parent religion texts.
        api.MapGet("/taxonomies/{id}", async (string id, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(id)) return Results.BadRequest();
            var node = await db.ReligionTaxonomies.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (node == null) return Results.NotFound();
            string[] parentTexts = Array.Empty<string>();
            if (node.Type == "sect" && !string.IsNullOrWhiteSpace(node.ParentId))
            {
                parentTexts = await db.ReligionTaxonomies.Where(t => t.Id == node.ParentId).Select(t => t.CanonicalTexts).FirstOrDefaultAsync(ct) ?? Array.Empty<string>();
            }
            var texts = parentTexts.Concat(node.CanonicalTexts).Distinct().ToArray();
            return Results.Ok(new { node.Id, node.DisplayName, node.Type, canonicalTexts = texts, terminology = node.DefaultTerminologyJson });
        });

        // Tenant settings (read)
        api.MapGet("/tenants/{id:guid}/settings", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            Guid id,
            AppDbContext db,
            ITerminologyService terms,
            CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FindAsync(new object?[] { id }, ct);
            if (tenant == null) return Results.NotFound();
            var resolved = await terms.GetResolvedAsync(id, tenant.TaxonomyId, ct);
            return Results.Ok(new TenantSettingsDto(tenant.Id, tenant.Name, tenant.Slug, tenant.Status, tenant.TaxonomyId, resolved));
        });

        // Tenant update (name, taxonomy, status)
        api.MapPut("/tenants/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            Guid id,
            [FromBody] TenantUpdateRequest req,
            AppDbContext db,
            IAuditWriter audit,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FindAsync(new object?[] { id }, ct);
            if (tenant == null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(req.Name)) tenant.Name = req.Name.Trim();
            if (!string.IsNullOrWhiteSpace(req.TaxonomyId)) tenant.TaxonomyId = req.TaxonomyId;
            if (!string.IsNullOrWhiteSpace(req.Status)) tenant.Status = req.Status;
            await db.SaveChangesAsync(ct);
            var actorId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(actorId, out var actorGuid))
            {
                await audit.WriteAsync(tenant.Id, actorGuid, "TENANT_UPDATED", "tenant", tenant.Id.ToString(), new { tenant.Name, tenant.TaxonomyId, tenant.Status }, ct);
            }
            return Results.Ok(new { tenant.Id, tenant.Name, tenant.Slug, tenant.Status, tenant.TaxonomyId });
        });

        api.MapPost("/auth/register", async (
            [FromBody] RegisterRequest request,
            AppDbContext db,
            PasswordHasher<User> hasher,
            TenantContext tenantCtx,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Results.BadRequest();
            // Basic password complexity (min 8, digit, upper, lower)
            if (request.Password.Length < 8 || !request.Password.Any(char.IsDigit) || !request.Password.Any(char.IsUpper) || !request.Password.Any(char.IsLower))
                return Results.BadRequest("Password does not meet complexity requirements");
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (existing != null) return Results.Conflict();
            var tenantId = tenantCtx.TenantId ?? (await db.Tenants.OrderBy(t => t.CreatedUtc).Select(t => t.Id).FirstOrDefaultAsync(ct));
            if (tenantId == Guid.Empty) return Results.BadRequest("No tenant context available");
            var user = new User { Email = request.Email, TenantId = tenantId };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            db.Users.Add(user);
            // Attach default member role so capability policies function immediately
            db.TenantUsers.Add(new TenantUser { TenantId = tenantId, UserId = user.Id, RoleKey = RoleCapabilities.Member });
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { id = user.Id });
        });

    api.MapPost("/auth/login", async (
            [FromBody] LoginRequest request,
            AppDbContext db,
            PasswordHasher<User> hasher,
            JwtSecurityTokenHandler tokenHandler,
            ICapabilityHashProvider hashProvider,
            IAuditWriter audit,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email, ct);
            if (user == null)
            {
                await audit.WriteAsync(Guid.Empty, null, "AUTH_FAIL", "user", request.Email, new { reason = "not_found" }, ct);
                return Results.Unauthorized();
            }
            var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verify == PasswordVerificationResult.Failed)
            {
                await audit.WriteAsync(user.TenantId, user.Id, "AUTH_FAIL", "user", user.Id.ToString(), new { reason = "bad_password" }, ct);
                return Results.Unauthorized();
            }
            // Temporary: allow login if unverified but created within last 48h (grace period until email service live)
            if (user.EmailVerifiedUtc == null && user.CreatedUtc < DateTime.UtcNow.AddHours(-48))
            {
                await audit.WriteAsync(user.TenantId, user.Id, "AUTH_FAIL", "user", user.Id.ToString(), new { reason = "email_unverified" }, ct);
                return Results.BadRequest(new { error = "EMAIL_NOT_VERIFIED" });
            }
            var tokens = await IssueTokensAsync(user, db, config, tokenHandler, hashProvider, ct);
            await audit.WriteAsync(user.TenantId, user.Id, "AUTH_SUCCESS", "user", user.Id.ToString(), null, ct);
            return Results.Ok(tokens);
    }).RequireRateLimiting("auth-login");

        // Create a guest user (lightweight, no email, limited capabilities) and issue tokens.
        api.MapPost("/auth/guest", async (
            AppDbContext db,
            IConfiguration config,
            JwtSecurityTokenHandler tokenHandler,
            ICapabilityHashProvider hashProvider,
            TenantContext tenantCtx,
            CancellationToken ct) =>
        {
            var tenantId = tenantCtx.TenantId ?? (await db.Tenants.OrderBy(t => t.CreatedUtc).Select(t => t.Id).FirstOrDefaultAsync(ct));
            if (tenantId == Guid.Empty) return Results.BadRequest("No tenant context");
            // Generate placeholder unique email surrogate (not used for outbound) to satisfy uniqueness.
            var surrogate = $"guest_{Guid.NewGuid():N}@guest.local";
            var user = new User { TenantId = tenantId, Email = surrogate, IsGuest = true, EmailVerifiedUtc = DateTime.UtcNow };
            user.PasswordHash = ""; // no password for guests
            db.Users.Add(user);
            // Assign guest role for capability filtering
            db.TenantUsers.Add(new TenantUser { TenantId = tenantId, UserId = user.Id, RoleKey = RoleCapabilities.Guest });
            await db.SaveChangesAsync(ct);
            var tokens = await IssueTokensAsync(user, db, config, tokenHandler, hashProvider, ct);
            return Results.Ok(tokens);
        });

        // Upgrade a guest to a full account by setting email & password (and clearing guest flag) without losing data.
        api.MapPost("/auth/guest/upgrade", [Microsoft.AspNetCore.Authorization.Authorize] async (
            ClaimsPrincipal principal,
            AppDbContext db,
            [FromBody] GuestUpgradeRequest req,
            PasswordHasher<User> hasher,
            CancellationToken ct) =>
        {
            var idStr = principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(idStr, out var uid)) return Results.Unauthorized();
            var user = await db.Users.FindAsync(new object?[] { uid }, ct);
            if (user == null || !user.IsGuest) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password)) return Results.BadRequest();
            var existing = await db.Users.AnyAsync(u => u.Email == req.Email && u.Id != user.Id, ct);
            if (existing) return Results.Conflict(new { error = "EMAIL_IN_USE" });
            user.Email = req.Email.Trim();
            user.PasswordHash = hasher.HashPassword(user, req.Password);
            user.IsGuest = false;
            user.EmailVerifiedUtc = null; // require verification flow later
            user.VerificationToken = Guid.NewGuid().ToString("N");
            user.VerificationTokenExpiresUtc = DateTime.UtcNow.AddHours(24);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { upgraded = true });
        });

        // Current profile (planned GET /api/users/me)
        api.MapGet("/users/me", [Microsoft.AspNetCore.Authorization.Authorize] async (
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var idStr = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(idStr, out var userId)) return Results.Unauthorized();
            var entity = await db.Users.FindAsync(new object?[] { userId }, ct);
            if (entity == null) return Results.NotFound();
            return Results.Ok(new { entity.Id, entity.Email, entity.DisplayName });
        });

        api.MapPost("/auth/refresh", async (
            [FromBody] RefreshRequest req,
            AppDbContext db,
            JwtSecurityTokenHandler handler,
            ICapabilityHashProvider hashProvider,
            IConfiguration config,
            CancellationToken ct) =>
        {
            var existing = await db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == req.RefreshToken, ct);
            if (existing == null || !existing.IsActive) return Results.Unauthorized();
            var user = await db.Users.FindAsync(new object?[] { existing.UserId }, ct);
            if (user == null) return Results.Unauthorized();
            // revoke old
            existing.RevokedUtc = DateTime.UtcNow;
            var tokens = await IssueTokensAsync(user, db, config, handler, hashProvider, ct);
            await db.SaveChangesAsync(ct);
            return Results.Ok(tokens);
        });

        // Schedule endpoints
        api.MapPost("/schedule/events", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleCreateEvent)] async (
            [FromBody] ScheduleEventCreate request,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            IAuditWriter audit,
            IEventReminderScheduler reminders,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest("No tenant context");
            var ev = new ScheduleEvent
            {
                TenantId = tenantCtx.TenantId.Value,
                Title = request.Title,
                StartUtc = request.StartUtc,
                EndUtc = request.EndUtc,
                Type = request.Type ?? "service",
                Description = request.Description
            };
            db.ScheduleEvents.Add(ev);
            var actorId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(actorId, out var actorGuid))
            {
                await audit.WriteAsync(tenantCtx.TenantId.Value, actorGuid, "EVENT_CREATED", "schedule_event", ev.Id.ToString(), new { ev.Title, ev.StartUtc, ev.EndUtc }, ct);
            }
            await db.SaveChangesAsync(ct);
            // Default reminder 60 minutes before if within future window
            await reminders.ScheduleAsync(tenantCtx.TenantId.Value, ev.Id, ev.StartUtc, new [] { 60 }, ct);
            return Results.Created($"/api/v1/schedule/events/{ev.Id}", ev);
        });

        // Extended create with recurrence & category. If recurrence rule provided, store master event only; optional expansion endpoint handles instances.
        api.MapPost("/schedule/events/extended", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleCreateEvent)] async (
            [FromBody] ScheduleEventCreateExtended request,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            IAuditWriter audit,
            IEventReminderScheduler reminders,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest("No tenant context");
            var seriesId = string.IsNullOrWhiteSpace(request.RecurrenceRule) ? (Guid?)null : Guid.NewGuid();
            var ev = new ScheduleEvent
            {
                TenantId = tenantCtx.TenantId.Value,
                Title = request.Title,
                StartUtc = request.StartUtc,
                EndUtc = request.EndUtc,
                Type = request.Type ?? "service",
                Description = request.Description,
                Category = request.Category,
                RecurrenceRule = request.RecurrenceRule,
                RecurrenceEndUtc = request.RecurrenceEndUtc,
                SeriesId = seriesId
            };
            db.ScheduleEvents.Add(ev);
            var actorId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(actorId, out var actorGuid))
                await audit.WriteAsync(tenantCtx.TenantId.Value, actorGuid, "EVENT_CREATED", "schedule_event", ev.Id.ToString(), new { ev.Title, ev.StartUtc, ev.EndUtc, ev.RecurrenceRule }, ct);
            await db.SaveChangesAsync(ct);
            // Schedule reminders: 1440 (1 day) & 60 minutes if recurrence master only for the first instance
            await reminders.ScheduleAsync(tenantCtx.TenantId.Value, ev.Id, ev.StartUtc, new [] { 1440, 60 }, ct);
            return Results.Created($"/api/v1/schedule/events/{ev.Id}", ev);
        });

        // Update event (manage capability)
        api.MapPut("/schedule/events/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleManageEvent)] async (
            Guid id,
            [FromBody] ScheduleEventUpdate update,
            TenantContext tenantCtx,
            AppDbContext db,
            IAuditWriter audit,
            ClaimsPrincipal user,
            IEventReminderScheduler reminders,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var ev = await db.ScheduleEvents.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantCtx.TenantId, ct);
            if (ev == null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(update.Title)) ev.Title = update.Title;
            if (update.StartUtc != null) ev.StartUtc = update.StartUtc.Value;
            if (update.EndUtc != null) ev.EndUtc = update.EndUtc.Value;
            if (!string.IsNullOrWhiteSpace(update.Type)) ev.Type = update.Type!;
            if (update.Description != null) ev.Description = update.Description;
            if (update.Category != null) ev.Category = update.Category;
            if (update.RecurrenceRule != null) ev.RecurrenceRule = update.RecurrenceRule;
            if (update.RecurrenceEndUtc != null) ev.RecurrenceEndUtc = update.RecurrenceEndUtc;
            await db.SaveChangesAsync(ct);
            await reminders.RescheduleAsync(tenantCtx.TenantId.Value, ev.Id, ev.StartUtc, null, ct);
            var actorId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(actorId, out var actorGuid))
                await audit.WriteAsync(tenantCtx.TenantId.Value, actorGuid, "EVENT_UPDATED", "schedule_event", ev.Id.ToString(), new { ev.Title, ev.StartUtc, ev.EndUtc, ev.Category, ev.RecurrenceRule }, ct);
            return Results.Ok(ev);
        });

        // Delete event
        api.MapDelete("/schedule/events/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleManageEvent)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            IAuditWriter audit,
            IEventReminderScheduler reminders,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var ev = await db.ScheduleEvents.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantCtx.TenantId, ct);
            if (ev == null) return Results.NotFound();
            db.ScheduleEvents.Remove(ev);
            await db.SaveChangesAsync(ct);
            await reminders.CancelAsync(tenantCtx.TenantId.Value, id, ct);
            var actorId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(actorId, out var actorGuid))
                await audit.WriteAsync(tenantCtx.TenantId.Value, actorGuid, "EVENT_DELETED", "schedule_event", id.ToString(), null, ct);
            return Results.NoContent();
        });

        // Expand recurrence instances (computed, not persisted) limited preview
        api.MapGet("/schedule/events/{id:guid}/expand", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleRead)] async (
            Guid id,
            int? max,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var ev = await db.ScheduleEvents.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantCtx.TenantId, ct);
            if (ev == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(ev.RecurrenceRule)) return Results.Ok(new { master = ev, instances = Array.Empty<object>() });
            // Minimal RRULE parser: support FREQ=DAILY|WEEKLY;INTERVAL=n
            var ruleParts = ev.RecurrenceRule.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(p => p.Split('=')).Where(a => a.Length == 2).ToDictionary(a => a[0].ToUpperInvariant(), a => a[1]);
            var freq = ruleParts.TryGetValue("FREQ", out var f) ? f.ToUpperInvariant() : "DAILY";
            var interval = 1;
            if (ruleParts.TryGetValue("INTERVAL", out var iv) && int.TryParse(iv, out var parsed) && parsed > 0) interval = parsed;
            var until = ev.RecurrenceEndUtc ?? DateTime.UtcNow.AddMonths(3);
            var list = new List<object>();
            var start = ev.StartUtc;
            var end = ev.EndUtc;
            int cap = Math.Clamp(max ?? 50, 1, 200);
            while (start <= until && list.Count < cap)
            {
                list.Add(new { startUtc = start, endUtc = end });
                start = freq switch
                {
                    "DAILY" => start.AddDays(interval),
                    "WEEKLY" => start.AddDays(7 * interval),
                    _ => start.AddDays(interval)
                };
                end = freq switch
                {
                    "DAILY" => end.AddDays(interval),
                    "WEEKLY" => end.AddDays(7 * interval),
                    _ => end.AddDays(interval)
                };
            }
            return Results.Ok(new { master = ev, instances = list });
        });

        api.MapGet("/schedule/events", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ScheduleRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            int page, int pageSize,
            CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <=0 or >100 ? 20 : pageSize;
            if (tenantCtx.TenantId == null) return Results.BadRequest("No tenant context");
            var query = db.ScheduleEvents.Where(e => e.TenantId == tenantCtx.TenantId);
            var total = await query.CountAsync(ct);
            var data = await query.OrderBy(e => e.StartUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        api.MapPost("/terminology/override", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            ITerminologyService terms,
            Dictionary<string,string> overrides,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest("No tenant context");
            var entity = new TerminologyOverride { TenantId = tenantCtx.TenantId.Value, OverridesJson = System.Text.Json.JsonSerializer.Serialize(overrides) };
            db.TerminologyOverrides.Add(entity);
            db.AuditEvents.Add(new AuditEvent { TenantId = tenantCtx.TenantId.Value, ActorUserId = Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!), Action = "TERMINOLOGY_OVERRIDE_SET", EntityType = "terminology", EntityId = entity.Id.ToString() });
            await db.SaveChangesAsync(ct);
            return Results.Ok(await terms.GetResolvedAsync(tenantCtx.TenantId.Value, (await db.Tenants.FindAsync(new object?[] { tenantCtx.TenantId.Value }, ct))?.TaxonomyId, ct));
        });

        api.MapPost("/auth/request-verification", async (AppDbContext db, [FromBody] EmailRequest req, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
            if (user == null) return Results.Ok(); // do not leak
            user.VerificationToken = Guid.NewGuid().ToString("N");
            user.VerificationTokenExpiresUtc = DateTime.UtcNow.AddHours(24);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { sent = true });
        });

        api.MapPost("/auth/verify", async (AppDbContext db, [FromBody] VerifyRequest req, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.VerificationToken == req.Token, ct);
            if (user == null || user.VerificationTokenExpiresUtc < DateTime.UtcNow) return Results.BadRequest();
            user.EmailVerifiedUtc = DateTime.UtcNow;
            user.VerificationToken = null; user.VerificationTokenExpiresUtc = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { verified = true });
        });

        api.MapPost("/auth/request-password-reset", async (AppDbContext db, [FromBody] EmailRequest req, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email, ct);
            if (user == null) return Results.Ok();
            user.PasswordResetToken = Guid.NewGuid().ToString("N");
            user.PasswordResetTokenExpiresUtc = DateTime.UtcNow.AddHours(2);
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { sent = true });
        });

        api.MapPost("/auth/reset-password", async (AppDbContext db, [FromBody] ResetPasswordRequest req, PasswordHasher<User> hasher, CancellationToken ct) =>
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email && u.PasswordResetToken == req.Token, ct);
            if (user == null || user.PasswordResetTokenExpiresUtc < DateTime.UtcNow) return Results.BadRequest();
            user.PasswordHash = hasher.HashPassword(user, req.NewPassword);
            user.PasswordResetToken = null; user.PasswordResetTokenExpiresUtc = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { reset = true });
        });

        api.MapGet("/daily-content/today", async (TenantContext tenantCtx, AppDbContext db, CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var tenant = await db.Tenants.FindAsync(new object?[] { tenantCtx.TenantId.Value }, ct);
            var taxonomy = tenant?.TaxonomyId;
            if (taxonomy == null) return Results.Ok(new { content = (string?)null });
            var pick = await db.DailyContents.Where(c => c.TaxonomyId == taxonomy && c.Active)
                .OrderBy(c => Guid.NewGuid())
                .Select(c => new { c.Type, c.Body })
                .FirstOrDefaultAsync(ct);
            return Results.Ok(pick);
        });

        api.MapGet("/audit/events", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgViewAudit)] async (
            TenantContext tenantCtx, AppDbContext db, int page, int pageSize, CancellationToken ct) =>
        {
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 50 : pageSize;
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var query = db.AuditEvents.Where(a => a.TenantId == tenantCtx.TenantId);
            var total = await query.CountAsync(ct);
            var data = await query.OrderByDescending(a => a.CreatedUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        // Automation rule list
        api.MapGet("/automation/rules", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.AutomationManageRules)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.AutomationRules.Where(r => r.TenantId == tenantCtx.TenantId).OrderBy(r => r.CreatedUtc).ToListAsync(ct);
            return Results.Ok(list);
        });

        // Automation rule create
        api.MapPost("/automation/rules", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.AutomationManageRules)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] AutomationRuleCreateRequest req,
            ClaimsPrincipal user,
            IAuditWriter audit,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var rule = new Temple.Domain.Automation.AutomationRule { TenantId = tenantCtx.TenantId.Value, TriggerType = req.TriggerType, ConditionJson = req.ConditionJson ?? "{}", ActionJson = req.ActionJson ?? "{}", Enabled = req.Enabled ?? true };
            db.AutomationRules.Add(rule);
            var actorId = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (Guid.TryParse(actorId, out var actorGuid))
                await audit.WriteAsync(tenantCtx.TenantId.Value, actorGuid, "AUTOMATION_RULE_CREATED", "automation_rule", rule.Id.ToString(), new { rule.TriggerType }, ct);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/automation/rules/{rule.Id}", rule);
        });

    app.MapHub<Temple.Api.Chat.ChatHub>("/hubs/chat");

    // Demo seed endpoint (development only) to create a rich tenant dataset
    if (app.Environment.IsDevelopment())
    {
    api.MapPost("/dev/seed/demo", async (AppDbContext db, PasswordHasher<User> hasher, ICapabilityHashProvider hashProvider, JwtSecurityTokenHandler tokenHandler, IConfiguration config, CancellationToken ct) =>
        {
            // Create tenant
            var tenant = new Temple.Domain.Tenants.Tenant { Name = "Demo Church", Slug = "demo", Status = "active" };
            if (!await db.Tenants.AnyAsync(t => t.Slug == tenant.Slug, ct))
            {
                db.Tenants.Add(tenant);
                await db.SaveChangesAsync(ct);
            }
            else
            {
                tenant = await db.Tenants.FirstAsync(t => t.Slug == tenant.Slug, ct);
            }

            // Create admin user
            var admin = await db.Users.FirstOrDefaultAsync(u => u.Email == "demo.admin@demo.local" && u.TenantId == tenant.Id, ct);
            if (admin == null)
            {
                admin = new User { Email = "demo.admin@demo.local", TenantId = tenant.Id, EmailVerifiedUtc = DateTime.UtcNow };
                admin.PasswordHash = hasher.HashPassword(admin, "DemoAdmin#123");
                db.Users.Add(admin);
                await db.SaveChangesAsync(ct);
                // Add TenantUser role mapping
                var tu = new Temple.Domain.Identity.TenantUser { TenantId = tenant.Id, UserId = admin.Id, RoleKey = RoleCapabilities.TenantOwner };
                db.TenantUsers.Add(tu);
            }

            // Create sample people & households
            if (!await db.People.AnyAsync(p => p.TenantId == tenant.Id, ct))
            {
                var people = new List<Temple.Domain.People.Person>();
                for (int i = 0; i < 10; i++)
                {
                    people.Add(new Temple.Domain.People.Person { TenantId = tenant.Id, FirstName = $"Person{i+1}", LastName = "Sample", Status = i < 2 ? "member" : "guest" });
                }
                db.People.AddRange(people);
                await db.SaveChangesAsync(ct);
                var household = new Temple.Domain.People.Household { TenantId = tenant.Id, Name = "Sample Household" };
                db.Households.Add(household);
                await db.SaveChangesAsync(ct);
                foreach (var p in people.Take(4))
                    db.HouseholdMembers.Add(new Temple.Domain.People.HouseholdMember { TenantId = tenant.Id, HouseholdId = household.Id, PersonId = p.Id, Relationship = "member" });
            }

            // Create finance goal & fund (+ ledger entries, donations)
            Temple.Domain.Finance.FinanceGoal? goal = await db.FinanceGoals.FirstOrDefaultAsync(f => f.TenantId == tenant.Id, ct);
            if (goal == null)
            {
                goal = new Temple.Domain.Finance.FinanceGoal { TenantId = tenant.Id, Name = "Building Fund", TargetAmount = 50000m, CurrentAmount = 0m };
                db.FinanceGoals.Add(goal);
            }
            Temple.Domain.Stewardship.StewardshipFund? generalFund = await db.StewardshipFunds.FirstOrDefaultAsync(f => f.TenantId == tenant.Id && f.Name == "General Fund", ct);
            if (generalFund == null)
            {
                generalFund = new Temple.Domain.Stewardship.StewardshipFund { TenantId = tenant.Id, Name = "General Fund", Balance = 0m };
                db.StewardshipFunds.Add(generalFund);
            }
            await db.SaveChangesAsync(ct);

            if (!await db.Donations.AnyAsync(d => d.TenantId == tenant.Id, ct))
            {
                // Seed a few donations (some to goal, some general)
                var donations = new List<Temple.Domain.Donations.Donation>();
                for (int i = 0; i < 8; i++)
                {
                    var amt = 1000 + i * 250; // cents
                    var d = new Temple.Domain.Donations.Donation { TenantId = tenant.Id, AmountCents = amt, Currency = "usd", CreatedUtc = DateTime.UtcNow.AddDays(-i), ProviderDonationId = Guid.NewGuid().ToString(), Status = "succeeded", Recurring = (i % 3 == 0) };
                    if (i % 2 == 0) d.FinanceGoalId = goal!.Id; else d.StewardshipFundId = generalFund!.Id;
                    donations.Add(d);
                }
                db.Donations.AddRange(donations);
                await db.SaveChangesAsync(ct);
                // Update aggregates
                goal!.CurrentAmount = await db.Donations.Where(x => x.TenantId == tenant.Id && x.FinanceGoalId == goal.Id).SumAsync(x => (decimal)x.AmountCents / 100m, ct);
                generalFund!.Balance = await db.Donations.Where(x => x.TenantId == tenant.Id && x.StewardshipFundId == generalFund.Id).SumAsync(x => (decimal)x.AmountCents / 100m, ct);
            }

            // Stewardship campaign & pledge
            if (!await db.StewardshipCampaigns.AnyAsync(c => c.TenantId == tenant.Id, ct))
            {
                var campaign = new Temple.Domain.Stewardship.StewardshipCampaign { TenantId = tenant.Id, Name = "2025 Missions", GoalAmount = 15000m, StartUtc = DateTime.UtcNow.AddDays(-30), EndUtc = DateTime.UtcNow.AddDays(60) };
                db.StewardshipCampaigns.Add(campaign);
                await db.SaveChangesAsync(ct);
                var anyPerson = await db.People.FirstAsync(p => p.TenantId == tenant.Id, ct);
                db.StewardshipCampaignPledges.Add(new Temple.Domain.Stewardship.StewardshipCampaignPledge { TenantId = tenant.Id, CampaignId = campaign.Id, PersonId = anyPerson.Id, Amount = 500m });
            }

            // Budget categories & expenses
            if (!await db.BudgetCategories.AnyAsync(b => b.TenantId == tenant.Id, ct))
            {
                var ops = new Temple.Domain.Finance.BudgetCategory { TenantId = tenant.Id, Key = "ops", Name = "Operations", BudgetAmountCents = 1_000_000, ActualAmountCents = 0 };
                var outreach = new Temple.Domain.Finance.BudgetCategory { TenantId = tenant.Id, Key = "outreach", Name = "Outreach", BudgetAmountCents = 800_000, ActualAmountCents = 0 };
                db.BudgetCategories.AddRange(ops, outreach);
                await db.SaveChangesAsync(ct);
                // Add expenses
                db.Expenses.Add(new Temple.Domain.Finance.Expense { TenantId = tenant.Id, BudgetCategoryId = ops.Id, Description = "Utilities", AmountCents = 75_000, Status = "approved", SubmittedUtc = DateTime.UtcNow.AddDays(-5), ApprovedUtc = DateTime.UtcNow.AddDays(-4) });
                db.Expenses.Add(new Temple.Domain.Finance.Expense { TenantId = tenant.Id, BudgetCategoryId = outreach.Id, Description = "Community Event Supplies", AmountCents = 120_000, Status = "approved", SubmittedUtc = DateTime.UtcNow.AddDays(-3), ApprovedUtc = DateTime.UtcNow.AddDays(-2) });
                await db.SaveChangesAsync(ct);
                // Update category actuals
                ops.ActualAmountCents = await db.Expenses.Where(e => e.TenantId == tenant.Id && e.BudgetCategoryId == ops.Id && e.Status == "approved").SumAsync(e => e.AmountCents, ct);
                outreach.ActualAmountCents = await db.Expenses.Where(e => e.TenantId == tenant.Id && e.BudgetCategoryId == outreach.Id && e.Status == "approved").SumAsync(e => e.AmountCents, ct);
            }

            // Groups & meetings
            if (!await db.Groups.AnyAsync(g => g.TenantId == tenant.Id, ct))
            {
                var group = new Temple.Domain.Groups.Group { TenantId = tenant.Id, Name = "Young Adults", Type = "study", IsOpenEnrollment = true };
                db.Groups.Add(group);
                await db.SaveChangesAsync(ct);
                db.GroupMeetings.Add(new Temple.Domain.Groups.GroupMeeting { TenantId = tenant.Id, GroupId = group.Id, StartUtc = DateTime.UtcNow.AddDays(7), EndUtc = DateTime.UtcNow.AddDays(7).AddHours(2) });
            }

            // Worship songs & set list
            if (!await db.Songs.AnyAsync(s => s.TenantId == tenant.Id, ct))
            {
                var song1 = new Temple.Domain.Worship.Song { TenantId = tenant.Id, Title = "Amazing Grace", DefaultKey = "G" };
                var song2 = new Temple.Domain.Worship.Song { TenantId = tenant.Id, Title = "Great Is Thy Faithfulness", DefaultKey = "C" };
                db.Songs.AddRange(song1, song2);
                await db.SaveChangesAsync(ct);
                var set = new Temple.Domain.Worship.SetList { TenantId = tenant.Id, Name = "Sunday Set" };
                db.SetLists.Add(set);
                await db.SaveChangesAsync(ct);
                db.SetListSongs.Add(new Temple.Domain.Worship.SetListSong { TenantId = tenant.Id, SetListId = set.Id, SongId = song1.Id });
                db.SetListSongs.Add(new Temple.Domain.Worship.SetListSong { TenantId = tenant.Id, SetListId = set.Id, SongId = song2.Id });
            }

            // Non-cash gift
            if (!await db.NonCashGifts.AnyAsync(g => g.TenantId == tenant.Id, ct))
            {
                db.NonCashGifts.Add(new Temple.Domain.Stewardship.NonCashGift { TenantId = tenant.Id, Description = "Used Piano", EstimatedValue = 1500m, ReceivedUtc = DateTime.UtcNow.AddDays(-10) });
            }

            // Recurring commitment
            if (!await db.RecurringCommitments.AnyAsync(r => r.TenantId == tenant.Id, ct))
            {
                db.RecurringCommitments.Add(new Temple.Domain.Finance.RecurringCommitment { TenantId = tenant.Id, AmountCents = 20_000, Frequency = "monthly", StartUtc = DateTime.UtcNow.AddMonths(-1), Active = true });
            }

            // Create schedule events
            if (!await db.ScheduleEvents.AnyAsync(e => e.TenantId == tenant.Id, ct))
            {
                db.ScheduleEvents.Add(new ScheduleEvent { TenantId = tenant.Id, Title = "Sunday Service", StartUtc = DateTime.UtcNow.AddDays(1).Date.AddHours(15), EndUtc = DateTime.UtcNow.AddDays(1).Date.AddHours(16), Type = "service" });
                db.ScheduleEvents.Add(new ScheduleEvent { TenantId = tenant.Id, Title = "Midweek Study", StartUtc = DateTime.UtcNow.AddDays(3).Date.AddHours(18), EndUtc = DateTime.UtcNow.AddDays(3).Date.AddHours(19), Type = "group" });
            }

            await db.SaveChangesAsync(ct);

            var tokens = await IssueTokensAsync(admin, db, config, tokenHandler, hashProvider, ct);
            return Results.Ok(new { tenant = new { tenant.Id, tenant.Name, tenant.Slug }, admin = new { admin.Email }, stats = new {
                people = await db.People.CountAsync(p => p.TenantId == tenant.Id, ct),
                donations = await db.Donations.CountAsync(d => d.TenantId == tenant.Id, ct),
                groups = await db.Groups.CountAsync(g => g.TenantId == tenant.Id, ct),
                songs = await db.Songs.CountAsync(s => s.TenantId == tenant.Id, ct)
            }, tokens });
        });
    }

    // Hangfire dashboard protected (super admin only)
    if (!app.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        // Dashboard temporarily disabled until authorization filter implemented
        // app.MapHangfireDashboard("/jobs");
    }

    // Lesson automation state endpoints
    api.MapGet("/automation/lesson/state", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.AutomationManageRules)] async (
        TenantContext tenantCtx,
        AppDbContext db,
        CancellationToken ct) =>
    {
        if (tenantCtx.TenantId == null) return Results.BadRequest();
        var state = await db.LessonAutomationStates.FirstOrDefaultAsync(s => s.TenantId == tenantCtx.TenantId, ct);
        if (state == null) { state = new Temple.Domain.Automation.LessonAutomationState { TenantId = tenantCtx.TenantId.Value }; db.LessonAutomationStates.Add(state); await db.SaveChangesAsync(ct); }
        return Results.Ok(state);
    });

    api.MapPost("/automation/lesson/override", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.AutomationManageRules)] async (
        TenantContext tenantCtx,
        AppDbContext db,
        [FromBody] LessonOverrideRequest req,
        CancellationToken ct) =>
    {
        if (tenantCtx.TenantId == null) return Results.BadRequest();
        var state = await db.LessonAutomationStates.FirstOrDefaultAsync(s => s.TenantId == tenantCtx.TenantId, ct);
        if (state == null) { state = new Temple.Domain.Automation.LessonAutomationState { TenantId = tenantCtx.TenantId.Value }; db.LessonAutomationStates.Add(state); }
        if (req.LessonId == null)
        {
            state.ManualOverride = false;
            state.OverrideSetUtc = null;
        }
        else
        {
            // validate lesson belongs to tenant and is published
            var lesson = await db.Lessons.FirstOrDefaultAsync(l => l.Id == req.LessonId && l.TenantId == tenantCtx.TenantId, ct);
            if (lesson == null) return Results.NotFound(new { message = "Lesson not found" });
            if (lesson.PublishedUtc == null) return Results.BadRequest(new { message = "Lesson must be published before overriding" });
            state.ActiveLessonId = lesson.Id;
            state.ManualOverride = true;
            state.OverrideSetUtc = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        return Results.Ok(state);
    });

        // Super admin global settings (platform-wide). Protected by super claim only.
        api.MapGet("/admin/global-settings", [Microsoft.AspNetCore.Authorization.Authorize] async (
            ClaimsPrincipal user,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!user.HasClaim("super", "1")) return Results.Forbid();
            var list = await db.GlobalSettings.OrderBy(g => g.Key).ToListAsync(ct);
            return Results.Ok(list.Select(g => new { g.Key, g.Value, g.UpdatedUtc }));
        });

        api.MapPut("/admin/global-settings/{key}", [Microsoft.AspNetCore.Authorization.Authorize] async (
            string key,
            [FromBody] GlobalSettingUpsertRequest req,
            ClaimsPrincipal user,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!user.HasClaim("super", "1")) return Results.Forbid();
            key = key.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(key)) return Results.BadRequest();
            var setting = await db.GlobalSettings.FirstOrDefaultAsync(g => g.Key == key, ct);
            Guid? actor = null; var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub); if (Guid.TryParse(sub, out var sg)) actor = sg;
            if (setting == null)
            {
                setting = new Temple.Domain.Configuration.GlobalSetting { Key = key, Value = req.Value ?? string.Empty, UpdatedByUserId = actor };
                db.GlobalSettings.Add(setting);
            }
            else
            {
                setting.Value = req.Value ?? string.Empty;
                setting.UpdatedUtc = DateTime.UtcNow;
                setting.UpdatedByUserId = actor;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { setting.Key, setting.Value, setting.UpdatedUtc });
        });

        api.MapDelete("/admin/global-settings/{key}", [Microsoft.AspNetCore.Authorization.Authorize] async (
            string key,
            ClaimsPrincipal user,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (!user.HasClaim("super", "1")) return Results.Forbid();
            key = key.Trim().ToLowerInvariant();
            var setting = await db.GlobalSettings.FirstOrDefaultAsync(g => g.Key == key, ct);
            if (setting == null) return Results.NotFound();
            db.GlobalSettings.Remove(setting);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        // Custom Roles (tenant-scoped) - manage capability sets
        api.MapGet("/roles/custom", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.CustomRoles.Where(r => r.TenantId == tenantCtx.TenantId).OrderBy(r => r.Key).ToListAsync(ct);
            return Results.Ok(list.Select(r => new { r.Id, r.Key, r.Name, r.Capabilities, r.System }));
        });

        api.MapPost("/roles/custom", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] CustomRoleCreateRequest req,
            ICapabilityHashRegenerator hashRegen,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Key) || string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest();
            var key = req.Key.Trim().ToLowerInvariant();
            var exists = await db.CustomRoles.AnyAsync(r => r.TenantId == tenantCtx.TenantId && r.Key == key, ct);
            if (exists) return Results.Conflict();
            var caps = (req.Capabilities ?? Array.Empty<string>()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var role = new CustomRole { TenantId = tenantCtx.TenantId.Value, Key = key, Name = req.Name.Trim(), Capabilities = caps };
            db.CustomRoles.Add(role);
            await db.SaveChangesAsync(ct);
            await hashRegen.RegenerateAsync(tenantCtx.TenantId.Value, ct);
            return Results.Created($"/api/v1/roles/custom/{role.Id}", new { role.Id, role.Key, role.Name, role.Capabilities });
        });

        api.MapPut("/roles/custom/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] CustomRoleUpdateRequest req,
            ICapabilityHashRegenerator hashRegen,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var role = await db.CustomRoles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantCtx.TenantId, ct);
            if (role == null || role.System) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(req.Name)) role.Name = req.Name.Trim();
            if (req.Capabilities != null) role.Capabilities = req.Capabilities.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            role.UpdatedUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            await hashRegen.RegenerateAsync(tenantCtx.TenantId.Value, ct);
            return Results.Ok(new { role.Id, role.Key, role.Name, role.Capabilities });
        });

        api.MapDelete("/roles/custom/{id:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            ICapabilityHashRegenerator hashRegen,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var role = await db.CustomRoles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == tenantCtx.TenantId, ct);
            if (role == null || role.System) return Results.NotFound();
            db.CustomRoles.Remove(role);
            await db.SaveChangesAsync(ct);
            await hashRegen.RegenerateAsync(tenantCtx.TenantId.Value, ct);
            return Results.NoContent();
        });

        // People & Households (initial scaffold)
        api.MapPost("/people", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] PersonCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = new Temple.Domain.People.Person { TenantId = tenantCtx.TenantId.Value, FirstName = req.FirstName, LastName = req.LastName, Email = req.Email, Phone = req.Phone, Status = req.Status ?? "guest", BirthDate = req.BirthDate };
            db.People.Add(person);

        // Assimilation: status transition (guest->attender->member)
    api.MapPost("/people/{id:guid}/status", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.PeopleRecordAttendance)] async (
            Guid id,
            [FromBody] PersonStatusRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            IAuditWriter audit,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            var allowed = new[] { "guest", "attender", "member" };
            if (string.IsNullOrWhiteSpace(req.Status) || !allowed.Contains(req.Status)) return Results.BadRequest(new { message = "Invalid status" });
            // enforce forward-only progression
            int Rank(string s) => Array.IndexOf(allowed, s);
            if (Rank(req.Status) < Rank(person.Status)) return Results.BadRequest(new { message = "Cannot regress status" });
            if (person.Status != req.Status)
            {
                person.Status = req.Status;
                person.UpdatedUtc = DateTime.UtcNow;
                var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
                if (Guid.TryParse(sub, out var actor))
                    await audit.WriteAsync(tenantCtx.TenantId.Value, actor, "PERSON_STATUS_UPDATED", "person", person.Id.ToString(), new { person.Status }, ct);
                await db.SaveChangesAsync(ct);
            }
            return Results.Ok(new { person.Id, person.Status });
        });

        // Attendance record
    api.MapPost("/people/{id:guid}/attendance", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.PeopleRecordAttendance)] async (
            Guid id,
            [FromBody] AttendanceRecordRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            var rec = new Temple.Domain.People.AttendanceRecord { TenantId = tenantCtx.TenantId.Value, PersonId = person.Id, ScheduleEventId = req.ScheduleEventId, DateUtc = req.DateUtc?.ToUniversalTime() ?? DateTime.UtcNow, Source = req.Source };
            db.AttendanceRecords.Add(rec);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/people/{id}/attendance/{rec.Id}", rec);
        });

        // Milestone
    api.MapPost("/people/{id:guid}/milestones", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.PeopleAddMilestone)] async (
            Guid id,
            [FromBody] MilestoneRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Type)) return Results.BadRequest(new { message = "Type required" });
            var ms = new Temple.Domain.People.Milestone { TenantId = tenantCtx.TenantId.Value, PersonId = person.Id, Type = req.Type.Trim().ToLowerInvariant(), DateUtc = (req.DateUtc ?? DateTime.UtcNow).ToUniversalTime(), Notes = req.Notes };
            db.Milestones.Add(ms);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/people/{id}/milestones/{ms.Id}", ms);
        });

        // Prayer request
    api.MapPost("/people/{id:guid}/prayer-requests", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.PeopleAddPrayerRequest)] async (
            Guid id,
            [FromBody] PrayerRequestCreate req,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            if (string.IsNullOrWhiteSpace(req.Title)) return Results.BadRequest(new { message = "Title required" });
            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(sub, out var actor)) return Results.Unauthorized();
            var pr = new Temple.Domain.People.PrayerRequest { TenantId = tenantCtx.TenantId.Value, PersonId = person.Id, CreatedByUserId = actor, Title = req.Title, Body = req.Body ?? string.Empty, Confidentiality = req.Confidentiality ?? "standard" };
            db.PrayerRequests.Add(pr);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/people/{id}/prayer-requests/{pr.Id}", pr);
        });

        // Pastoral care note
    api.MapPost("/people/{id:guid}/care-notes", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.PeopleAddCareNote)] async (
            Guid id,
            [FromBody] CareNoteCreate req,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            var sub = user.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(sub, out var actor)) return Results.Unauthorized();
            if (string.IsNullOrWhiteSpace(req.Note)) return Results.BadRequest(new { message = "Note required" });
            var note = new Temple.Domain.People.PastoralCareNote { TenantId = tenantCtx.TenantId.Value, PersonId = person.Id, CreatedByUserId = actor, Sensitivity = req.Sensitivity ?? "standard", Note = req.Note };
            db.PastoralCareNotes.Add(note);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/people/{id}/care-notes/{note.Id}", note);
        });

        // Groups & Discipleship (initial slice)
        api.MapPost("/groups", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.GroupsManage)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] GroupCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Name required" });
            var g = new Temple.Domain.Groups.Group { TenantId = tenantCtx.TenantId.Value, Name = req.Name.Trim(), Description = req.Description, Type = req.Type, IsOpenEnrollment = req.IsOpenEnrollment ?? true, Capacity = req.Capacity };
            db.Groups.Add(g);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/groups/{g.Id}", g);
        });

        api.MapGet("/groups", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.GroupsRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.Groups.Where(g => g.TenantId == tenantCtx.TenantId).OrderBy(g => g.Name).ToListAsync(ct);
            return Results.Ok(list);
        });

        api.MapPost("/groups/{id:guid}/enroll", [Microsoft.AspNetCore.Authorization.Authorize] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantCtx.TenantId, ct);
            if (group == null) return Results.NotFound();
            if (!group.IsOpenEnrollment && !user.HasClaim("cap", Capability.GroupsManage)) return Results.Forbid();
            // simplistic: map current user to a Person if exists via email match
            var email = user.FindFirstValue(JwtRegisteredClaimNames.Email);
            var person = email == null ? null : await db.People.FirstOrDefaultAsync(p => p.TenantId == tenantCtx.TenantId && p.Email == email, ct);
            if (person == null) return Results.BadRequest(new { message = "User not linked to person" });
            var exists = await db.GroupMembers.AnyAsync(m => m.TenantId == tenantCtx.TenantId && m.GroupId == id && m.PersonId == person.Id, ct);
            if (exists) return Results.Conflict();
            db.GroupMembers.Add(new Temple.Domain.Groups.GroupMember { TenantId = tenantCtx.TenantId.Value, GroupId = id, PersonId = person.Id, Role = "member" });
            await db.SaveChangesAsync(ct);
            return Results.Ok();
        });

        api.MapPost("/groups/{id:guid}/meetings", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.GroupsManage)] async (
            Guid id,
            [FromBody] GroupMeetingCreateRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var group = await db.Groups.FirstOrDefaultAsync(g => g.Id == id && g.TenantId == tenantCtx.TenantId, ct);
            if (group == null) return Results.NotFound();
            var start = req.StartUtc.ToUniversalTime();
            var meeting = new Temple.Domain.Groups.GroupMeeting { TenantId = tenantCtx.TenantId.Value, GroupId = id, StartUtc = start, EndUtc = req.EndUtc?.ToUniversalTime(), Notes = req.Notes };
            db.GroupMeetings.Add(meeting);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/groups/{id}/meetings/{meeting.Id}", meeting);
        });

        api.MapPost("/groups/{groupId:guid}/meetings/{meetingId:guid}/attendance", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.GroupsRecordAttendance)] async (
            Guid groupId,
            Guid meetingId,
            [FromBody] GroupMeetingAttendanceRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var meeting = await db.GroupMeetings.FirstOrDefaultAsync(m => m.Id == meetingId && m.GroupId == groupId && m.TenantId == tenantCtx.TenantId, ct);
            if (meeting == null) return Results.NotFound();
            if (req.PersonIds == null || req.PersonIds.Length == 0) return Results.BadRequest(new { message = "PersonIds required" });
            var existing = await db.GroupMeetingAttendances.Where(a => a.TenantId == tenantCtx.TenantId && a.MeetingId == meetingId).Select(a => a.PersonId).ToListAsync(ct);
            var toAdd = req.PersonIds.Distinct().Where(pid => !existing.Contains(pid)).ToList();
            foreach (var pid in toAdd)
            {
                db.GroupMeetingAttendances.Add(new Temple.Domain.Groups.GroupMeetingAttendance { TenantId = tenantCtx.TenantId.Value, GroupId = groupId, MeetingId = meetingId, PersonId = pid });
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { added = toAdd.Count });
        });

        // Worship: Songs
        api.MapPost("/songs", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.SongsManage)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] SongCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Title)) return Results.BadRequest(new { message = "Title required" });
            var song = new Temple.Domain.Worship.Song { TenantId = tenantCtx.TenantId.Value, Title = req.Title.Trim(), CcliNumber = req.CcliNumber, DefaultKey = req.DefaultKey, ArrangementNotes = req.ArrangementNotes };
            db.Songs.Add(song);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/songs/{song.Id}", song);
        });

        api.MapGet("/songs", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.SongsRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            string? q,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var qry = db.Songs.Where(s => s.TenantId == tenantCtx.TenantId);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLowerInvariant();
                qry = qry.Where(s => s.Title.ToLower().Contains(q));
            }
            var list = await qry.OrderBy(s => s.Title).Take(200).ToListAsync(ct);
            return Results.Ok(list);
        });

        // Worship: Service Plans
        api.MapPost("/service-plans", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ServicePlansManage)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] ServicePlanCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var plan = new Temple.Domain.Worship.ServicePlan { TenantId = tenantCtx.TenantId.Value, ServiceDateUtc = req.ServiceDateUtc.ToUniversalTime(), Title = req.Title, Notes = req.Notes };
            db.ServicePlans.Add(plan);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/service-plans/{plan.Id}", plan);
        });

        api.MapGet("/service-plans", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.ServicePlansRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            DateTime? fromUtc,
            DateTime? toUtc,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var qry = db.ServicePlans.Where(p => p.TenantId == tenantCtx.TenantId);
            if (fromUtc.HasValue) qry = qry.Where(p => p.ServiceDateUtc >= fromUtc.Value.ToUniversalTime());
            if (toUtc.HasValue) qry = qry.Where(p => p.ServiceDateUtc <= toUtc.Value.ToUniversalTime());
            var list = await qry.OrderBy(p => p.ServiceDateUtc).Take(200).ToListAsync(ct);
            return Results.Ok(list);
        });

        // Worship: Set Lists (simplified)
        api.MapPost("/setlists", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.SongsManage)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] SetListCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Name required" });
            var set = new Temple.Domain.Worship.SetList { TenantId = tenantCtx.TenantId.Value, Name = req.Name.Trim() };
            db.SetLists.Add(set);
            await db.SaveChangesAsync(ct);
            if (req.Songs != null)
            {
                int order = 0;
                foreach (var s in req.Songs)
                {
                    db.SetListSongs.Add(new Temple.Domain.Worship.SetListSong { TenantId = tenantCtx.TenantId.Value, SetListId = set.Id, SongId = s.SongId, Order = order++, Key = s.Key });
                }
                await db.SaveChangesAsync(ct);
            }
            return Results.Created($"/api/v1/setlists/{set.Id}", set);
        });

        api.MapGet("/setlists", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.SongsRead)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var sets = await db.SetLists.Where(s => s.TenantId == tenantCtx.TenantId).OrderByDescending(s => s.CreatedUtc).Take(100).ToListAsync(ct);
            return Results.Ok(sets);
        });

        // Volunteers: Positions
        api.MapPost("/volunteer/positions", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.VolunteersManagePositions)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] VolunteerPositionCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Name required" });
            var pos = new Temple.Domain.Volunteers.VolunteerPosition { TenantId = tenantCtx.TenantId.Value, Name = req.Name.Trim(), Description = req.Description };
            db.VolunteerPositions.Add(pos);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/volunteer/positions/{pos.Id}", pos);
        });

        api.MapGet("/volunteer/positions", [Microsoft.AspNetCore.Authorization.Authorize] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.VolunteerPositions.Where(p => p.TenantId == tenantCtx.TenantId && p.Active).OrderBy(p => p.Name).ToListAsync(ct);
            return Results.Ok(list);
        });

        // Volunteer assignment (add person to position)
        api.MapPost("/volunteer/positions/{id:guid}/assign", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.VolunteersAssign)] async (
            Guid id,
            [FromBody] VolunteerAssignmentCreate req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var pos = await db.VolunteerPositions.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantCtx.TenantId, ct);
            if (pos == null) return Results.NotFound();
            var assignment = new Temple.Domain.Volunteers.VolunteerAssignment { TenantId = tenantCtx.TenantId.Value, PositionId = id, PersonId = req.PersonId, StartUtc = req.StartUtc?.ToUniversalTime() ?? DateTime.UtcNow, EndUtc = req.EndUtc?.ToUniversalTime(), Notes = req.Notes };
            db.VolunteerAssignments.Add(assignment);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/volunteer/positions/{id}/assign/{assignment.Id}", assignment);
        });

        // Volunteer availability record (replace or create)
        api.MapPut("/volunteer/availability/{personId:guid}", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.VolunteersRecordAvailability)] async (
            Guid personId,
            [FromBody] VolunteerAvailabilityUpsert req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == personId && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            var avail = await db.VolunteerAvailabilities.FirstOrDefaultAsync(a => a.TenantId == tenantCtx.TenantId && a.PersonId == personId, ct);
            if (avail == null)
            {
                avail = new Temple.Domain.Volunteers.VolunteerAvailability { TenantId = tenantCtx.TenantId.Value, PersonId = personId, Pattern = req.Pattern ?? string.Empty };
                db.VolunteerAvailabilities.Add(avail);
            }
            else
            {
                avail.Pattern = req.Pattern ?? string.Empty;
                avail.UpdatedUtc = DateTime.UtcNow;
            }
            await db.SaveChangesAsync(ct);
            return Results.Ok(avail);
        });

        // Background check request
        api.MapPost("/volunteer/background-checks", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.VolunteersBackgroundCheck)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] BackgroundCheckRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == req.PersonId && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.NotFound();
            var check = new Temple.Domain.Volunteers.VolunteerBackgroundCheck { TenantId = tenantCtx.TenantId.Value, PersonId = req.PersonId, Reference = req.Reference };
            db.VolunteerBackgroundChecks.Add(check);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/volunteer/background-checks/{check.Id}", check);
        });

        // Stewardship: Campaigns
        api.MapPost("/stewardship/campaigns", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageCampaigns)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] StewardshipCampaignCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Name required" });
            var camp = new Temple.Domain.Stewardship.StewardshipCampaign { TenantId = tenantCtx.TenantId.Value, Name = req.Name.Trim(), Description = req.Description, GoalAmount = req.GoalAmount ?? 0m, StartUtc = req.StartUtc?.ToUniversalTime() ?? DateTime.UtcNow, EndUtc = req.EndUtc?.ToUniversalTime() };
            db.StewardshipCampaigns.Add(camp);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/stewardship/campaigns/{camp.Id}", camp);
        });

        api.MapGet("/stewardship/campaigns", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageCampaigns)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.StewardshipCampaigns.Where(c => c.TenantId == tenantCtx.TenantId && !c.IsArchived).OrderByDescending(c => c.CreatedUtc).Take(200).ToListAsync(ct);
            return Results.Ok(list);
        });

        api.MapPost("/stewardship/campaigns/{id:guid}/pledges", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageCampaigns)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] StewardshipCampaignPledgeCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var camp = await db.StewardshipCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantCtx.TenantId, ct);
            if (camp == null) return Results.NotFound();
            var pledge = new Temple.Domain.Stewardship.StewardshipCampaignPledge { TenantId = tenantCtx.TenantId.Value, CampaignId = id, PersonId = req.PersonId, Amount = req.Amount };
            db.StewardshipCampaignPledges.Add(pledge);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/stewardship/campaigns/{id}/pledges/{pledge.Id}", pledge);
        });

        api.MapPost("/stewardship/campaigns/{id:guid}/archive", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageCampaigns)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var camp = await db.StewardshipCampaigns.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantCtx.TenantId, ct);
            if (camp == null) return Results.NotFound();
            camp.IsArchived = true;
            await db.SaveChangesAsync(ct);
            return Results.Ok(camp);
        });

        // Stewardship: Funds
        api.MapPost("/stewardship/funds", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageFunds)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] StewardshipFundCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            if (string.IsNullOrWhiteSpace(req.Name)) return Results.BadRequest(new { message = "Name required" });
            var fund = new Temple.Domain.Stewardship.StewardshipFund { TenantId = tenantCtx.TenantId.Value, Name = req.Name.Trim(), Code = req.Code, Description = req.Description };
            db.StewardshipFunds.Add(fund);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/stewardship/funds/{fund.Id}", fund);
        });

        api.MapGet("/stewardship/funds", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageFunds)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.StewardshipFunds.Where(f => f.TenantId == tenantCtx.TenantId && !f.IsArchived).OrderBy(f => f.Name).ToListAsync(ct);
            return Results.Ok(list);
        });

        api.MapPost("/stewardship/funds/{id:guid}/ledger", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageFunds)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] StewardshipFundLedgerEntryCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var fund = await db.StewardshipFunds.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantCtx.TenantId, ct);
            if (fund == null) return Results.NotFound();
            var entry = new Temple.Domain.Stewardship.StewardshipFundLedgerEntry { TenantId = tenantCtx.TenantId.Value, FundId = id, CampaignId = req.CampaignId, DonationId = req.DonationId, Amount = req.Amount, Type = req.Type, Notes = req.Notes };
            db.StewardshipFundLedgerEntries.Add(entry);
            fund.Balance += req.Amount; // negative amounts reduce
            if (req.CampaignId.HasValue)
            {
                var camp = await db.StewardshipCampaigns.FirstOrDefaultAsync(c => c.Id == req.CampaignId && c.TenantId == tenantCtx.TenantId, ct);
                if (camp != null)
                {
                    camp.RaisedAmount += req.Amount; // allow negative adjustments
                    if (camp.RaisedAmount < 0) camp.RaisedAmount = 0;
                }
            }
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/stewardship/funds/{id}/ledger/{entry.Id}", entry);
        });

        api.MapGet("/stewardship/funds/{id:guid}/ledger", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipManageFunds)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            int? take,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var fund = await db.StewardshipFunds.FirstOrDefaultAsync(f => f.Id == id && f.TenantId == tenantCtx.TenantId, ct);
            if (fund == null) return Results.NotFound();
            var q = db.StewardshipFundLedgerEntries.Where(e => e.TenantId == tenantCtx.TenantId && e.FundId == id).OrderByDescending(e => e.CreatedUtc);
            var list = await q.Take(take is >0 and <500 ? take.Value : 100).ToListAsync(ct);
            return Results.Ok(new { fund.Balance, entries = list });
        });

        // Stewardship: Non-cash gifts
        api.MapPost("/stewardship/noncash-gifts", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipRecordNonCash)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] NonCashGiftCreate req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var gift = new Temple.Domain.Stewardship.NonCashGift { TenantId = tenantCtx.TenantId.Value, DonorPersonId = req.DonorPersonId, Description = req.Description, EstimatedValue = req.EstimatedValue, AppraisalDocumentUrl = req.AppraisalDocumentUrl, ReceivedUtc = req.ReceivedUtc?.ToUniversalTime() ?? DateTime.UtcNow, Notes = req.Notes };
            db.NonCashGifts.Add(gift);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/stewardship/noncash-gifts/{gift.Id}", gift);
        });

        api.MapGet("/stewardship/noncash-gifts", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.StewardshipRecordNonCash)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            int page, int pageSize,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >200 ? 50 : pageSize;
            var q = db.NonCashGifts.Where(g => g.TenantId == tenantCtx.TenantId);
            var total = await q.CountAsync(ct);
            var data = await q.OrderByDescending(g => g.ReceivedUtc).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        api.MapPost("/volunteer/background-checks/{id:guid}/complete", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.VolunteersBackgroundCheck)] async (
            Guid id,
            [FromBody] BackgroundCheckCompleteRequest req,
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var check = await db.VolunteerBackgroundChecks.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantCtx.TenantId, ct);
            if (check == null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(req.Status)) check.Status = req.Status;
            check.CompletedUtc = DateTime.UtcNow;
            check.ExpiresUtc = req.ExpiresUtc?.ToUniversalTime();
            await db.SaveChangesAsync(ct);
            return Results.Ok(check);
        });
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/people/{person.Id}", person);
        });

        api.MapGet("/people", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            int page, int pageSize,
            string? status,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            page = page <= 0 ? 1 : page; pageSize = pageSize is <=0 or >100 ? 50 : pageSize;
            var q = db.People.Where(p => p.TenantId == tenantCtx.TenantId);
            if (!string.IsNullOrWhiteSpace(status)) q = q.Where(p => p.Status == status);
            var total = await q.CountAsync(ct);
            var data = await q.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
            return Results.Ok(new { data, page, pageSize, total });
        });

        api.MapPost("/households", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] HouseholdCreateRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var hh = new Temple.Domain.People.Household { TenantId = tenantCtx.TenantId.Value, Name = req.Name };
            db.Households.Add(hh);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/households/{hh.Id}", hh);
        });

        api.MapPost("/households/{id:guid}/members", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            Guid id,
            TenantContext tenantCtx,
            AppDbContext db,
            [FromBody] HouseholdMemberAddRequest req,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var hh = await db.Households.FirstOrDefaultAsync(h => h.Id == id && h.TenantId == tenantCtx.TenantId, ct);
            if (hh == null) return Results.NotFound();
            var person = await db.People.FirstOrDefaultAsync(p => p.Id == req.PersonId && p.TenantId == tenantCtx.TenantId, ct);
            if (person == null) return Results.BadRequest(new { error = "PERSON_NOT_FOUND" });
            var existing = await db.HouseholdMembers.FirstOrDefaultAsync(m => m.TenantId == tenantCtx.TenantId && m.HouseholdId == id && m.PersonId == req.PersonId, ct);
            if (existing != null) return Results.Conflict();
            var member = new Temple.Domain.People.HouseholdMember { TenantId = tenantCtx.TenantId.Value, HouseholdId = id, PersonId = req.PersonId, Relationship = req.Relationship ?? "member" };
            db.HouseholdMembers.Add(member);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/households/{id}/members/{member.Id}", member);
        });

        api.MapGet("/households", [Microsoft.AspNetCore.Authorization.Authorize(Policy = Capability.OrgManageSettings)] async (
            TenantContext tenantCtx,
            AppDbContext db,
            CancellationToken ct) =>
        {
            if (tenantCtx.TenantId == null) return Results.BadRequest();
            var list = await db.Households.Where(h => h.TenantId == tenantCtx.TenantId).OrderBy(h => h.Name).ToListAsync(ct);
            return Results.Ok(list);
        });

    // Schedule recurring jobs
    if (!app.Configuration.GetValue<bool>("UseInMemoryDatabase"))
    {
        using var scope = app.Services.CreateScope();
        var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        recurring.AddOrUpdate<IDailyContentRotationJob>("daily-content-rotation", job => job.RunAsync(CancellationToken.None), Cron.Daily(2));
        recurring.AddOrUpdate<ILessonRotationJob>("lesson-rotation", job => job.RunAsync(CancellationToken.None), Cron.Hourly());
    }

    app.Run();
    }

    static async Task<object> IssueTokensAsync(User user, AppDbContext db, IConfiguration config, JwtSecurityTokenHandler tokenHandler, ICapabilityHashProvider hashProvider, CancellationToken ct)
    {
        var jwtOpts = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOpts.Secret ?? "dev-secret-change"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("tid", user.TenantId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email)
        };
        var tu = await db.TenantUsers.FirstOrDefaultAsync(t => t.UserId == user.Id && t.TenantId == user.TenantId, ct);
        if (tu == null)
        {
            // Self-heal: attach a default member role if missing (legacy users / schema drift)
            tu = new TenantUser { TenantId = user.TenantId, UserId = user.Id, RoleKey = user.IsGuest ? RoleCapabilities.Guest : RoleCapabilities.Member };
            db.TenantUsers.Add(tu);
            await db.SaveChangesAsync(ct);
        }
        foreach (var cap in RoleCapabilities.Get(tu.RoleKey)) claims.Add(new Claim("cap", cap));
    if (user.IsSuperAdmin) claims.Add(new Claim("super", "1"));
        string capHash;
        try
        {
            capHash = await hashProvider.GetForTenantAsync(user.TenantId, ct);
        }
        catch
        {
            // Fallback if RoleVersions table or related schema not yet present (avoid 500 during bootstrap)
            capHash = "bootstrap";
        }
        claims.Add(new Claim("cap_hash", capHash));
        var jwtOptsExpiry = DateTime.UtcNow.AddMinutes(jwtOpts.ExpiryMinutes);
        var token = new JwtSecurityToken(
            issuer: jwtOpts.Issuer,
            audience: jwtOpts.Audience,
            claims: claims,
            expires: jwtOptsExpiry,
            signingCredentials: creds);
        var accessToken = tokenHandler.WriteToken(token);
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresUtc = DateTime.UtcNow.AddDays(30)
        };
        db.RefreshTokens.Add(refresh);
        await db.SaveChangesAsync(ct);
        return new { accessToken, refreshToken = refresh.Token };
    }
}

public record RegisterRequest(string Email, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshRequest(string RefreshToken);
public record ScheduleEventCreate(string Title, DateTime StartUtc, DateTime EndUtc, string? Type, string? Description);
public record ScheduleEventCreateExtended(string Title, DateTime StartUtc, DateTime EndUtc, string? Type, string? Description, string? Category, string? RecurrenceRule, DateTime? RecurrenceEndUtc);
public record ScheduleEventUpdate(string? Title, DateTime? StartUtc, DateTime? EndUtc, string? Type, string? Description, string? Category, string? RecurrenceRule, DateTime? RecurrenceEndUtc);
public record EmailRequest(string Email);
public record VerifyRequest(string Email, string Token);
public record ResetPasswordRequest(string Email, string Token, string NewPassword);
public record DonationCreateRequest(long AmountCents, string? Currency, bool Recurring, Guid? FinanceGoalId, Guid? StewardshipFundId);
public record ProfileUpdateRequest(string? DisplayName);
public record RoleLabelRequest(string? Label);
public record LessonOverrideRequest(Guid? LessonId);
public record PersonStatusRequest(string Status);
public record AttendanceRecordRequest(Guid? ScheduleEventId, DateTime? DateUtc, string? Source);
public record MilestoneRequest(string Type, DateTime? DateUtc, string? Notes);
public record PrayerRequestCreate(string Title, string? Body, string? Confidentiality);
public record CareNoteCreate(string Note, string? Sensitivity);
public record GroupCreateRequest(string Name, string? Description, string? Type, bool? IsOpenEnrollment, int? Capacity);
public record GroupMeetingCreateRequest(DateTime StartUtc, DateTime? EndUtc, string? Notes);
public record GroupMeetingAttendanceRequest(Guid[] PersonIds);
public record SongCreateRequest(string Title, string? CcliNumber, string? DefaultKey, string? ArrangementNotes);
public record ServicePlanCreateRequest(DateTime ServiceDateUtc, string? Title, string? Notes);
public record SetListCreateRequest(string Name, SetListSongRequest[]? Songs);
public record SetListSongRequest(Guid SongId, string? Key);
public record VolunteerPositionCreate(string Name, string? Description);
public record VolunteerAssignmentCreate(Guid PersonId, DateTime? StartUtc, DateTime? EndUtc, string? Notes);
public record VolunteerAvailabilityUpsert(string? Pattern);
public record BackgroundCheckRequest(Guid PersonId, string? Reference);
public record BackgroundCheckCompleteRequest(string? Status, DateTime? ExpiresUtc);
public record StewardshipCampaignCreate(string Name, string? Description, decimal? GoalAmount, DateTime? StartUtc, DateTime? EndUtc);
public record StewardshipCampaignPledgeCreate(Guid PersonId, decimal Amount);
public record StewardshipFundCreate(string Name, string? Code, string? Description);
public record StewardshipFundLedgerEntryCreate(decimal Amount, string? Type, string? Notes, Guid? CampaignId, Guid? DonationId);
public record NonCashGiftCreate(Guid? DonorPersonId, string Description, decimal? EstimatedValue, string? AppraisalDocumentUrl, DateTime? ReceivedUtc, string? Notes);
public record FinanceGoalCreate(string Key, string Name, string? Description, decimal? TargetAmount, DateTime? StartUtc, DateTime? EndUtc);
public record BudgetCategoryCreate(string Key, string Name, string? PeriodKey, long? BudgetAmountCents);
public record ExpenseSubmitRequest(Guid BudgetCategoryId, long AmountCents, string? Description);
public record ExpenseRejectRequest(string Reason);
public record RecurringCommitmentCreate(long AmountCents, string? Frequency, Guid? FinanceGoalId, Guid? StewardshipFundId, string? Notes);

// Hangfire dashboard authorization filter: restrict /jobs dashboard to super admins
public sealed class SuperAdminDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var http = context.GetHttpContext();
        if (http.User?.Identity?.IsAuthenticated != true) return false;
        return http.User.HasClaim(c => c.Type == "super" && c.Value == "1");
    }
}
public record GuestUpgradeRequest(string Email, string Password);
public record GlobalSettingUpsertRequest(string? Value);
public record CustomRoleCreateRequest(string Key, string Name, string[]? Capabilities);
public record CustomRoleUpdateRequest(string? Name, string[]? Capabilities);
public record NotificationSendRequest(Guid? UserId, string? Channel, string? Subject, string? Body);
public record NotificationPreferenceRequest(string Channel, bool Enabled);
public record ChatChannelCreateRequest(string Key, string Name, string? Description, bool IsPrivate);
public record ChatMessageCreateRequest(string? Body);
public record AutomationRuleCreateRequest(string TriggerType, string? ConditionJson, string? ActionJson, bool? Enabled);
public record LessonCreateRequest(string Title, string? Body, string[]? Tags);
public record LessonUpdateRequest(string? Title, string? Body, string[]? Tags);
public record MediaAssetCreateRequest(string Title, string? Type, string? StorageKey);
public record MediaUploadUrlRequest(string? Extension, string? MimeType);
public record LessonAttachMediaRequest(Guid MediaAssetId);
public record EventAttachMediaRequest(Guid MediaAssetId);
public record PersonCreateRequest(string FirstName, string LastName, string? Email, string? Phone, string? Status, DateTime? BirthDate);
public record HouseholdCreateRequest(string Name);
public record HouseholdMemberAddRequest(Guid PersonId, string? Relationship);

public class JwtOptions
{
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? Secret { get; set; }
    public int ExpiryMinutes { get; set; } = 60;
}

public static class FinanceDashboardHelper
{
    public static string GenerateEncouragementMessage(long lifetimeTotalCents, long periodTotalCents, IEnumerable<Temple.Domain.Finance.FinanceGoal> goals, IEnumerable<Temple.Domain.Stewardship.StewardshipFund> funds)
    {
        decimal lifetime = lifetimeTotalCents / 100m;
        decimal period = periodTotalCents / 100m;
        var topGoal = goals.OrderByDescending(g => g.TargetAmount == 0 ? 0 : g.CurrentAmount / g.TargetAmount).FirstOrDefault();
        string goalLine = topGoal == null ? "Set a goal to focus generosity." : $"Top goal '{topGoal.Name}' is at {(topGoal.TargetAmount==0?0: Math.Round((double)(topGoal.CurrentAmount/topGoal.TargetAmount)*100,1))}%";
        decimal totalFundBalance = funds.Sum(f => f.Balance);
        var phrases = new List<string>();
        phrases.Add($"Thank you! Lifetime generosity has reached {lifetime:C}.");
        if (period > 0) phrases.Add($"In the recent period: {period:C} given.");
        phrases.Add(goalLine + ".");
        phrases.Add($"Current designated fund balance: {totalFundBalance:C} supporting ongoing ministry.");
        if (period > 0 && lifetime > 0 && period / lifetime > 0.1m) phrases.Add("Momentum is strong this season – keep it up!");
        if (topGoal != null && topGoal.TargetAmount > 0 && (topGoal.TargetAmount - topGoal.CurrentAmount) <= topGoal.TargetAmount * 0.1m) phrases.Add($"Almost there for '{topGoal.Name}' – final stretch!");
        return string.Join(' ', phrases);
    }
}

public class SeedStartupData : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<SeedStartupData> _logger;
    public SeedStartupData(IServiceProvider sp, ILogger<SeedStartupData> logger) { _sp = sp; _logger = logger; }
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();

        await SeedTaxonomyAsync(db, cancellationToken);
        await SeedDailyContentAsync(db, cancellationToken);

        if (!await db.Tenants.AnyAsync(cancellationToken))
        {
            var tenant = new Temple.Domain.Tenants.Tenant { Name = "Example Community", Slug = "example", Status = "active", CreatedUtc = DateTime.UtcNow };
            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded default tenant {Tenant}", tenant.Id);
        }
        var firstTenant = await db.Tenants.OrderBy(t => t.CreatedUtc).FirstAsync(cancellationToken);
        // Seed system chat channels if none
        if (!await db.ChatChannels.AnyAsync(c => c.TenantId == firstTenant.Id, cancellationToken))
        {
            db.ChatChannels.Add(new Temple.Domain.Chat.ChatChannel { TenantId = firstTenant.Id, Key = "general", Name = "General", IsSystem = true });
            db.ChatChannels.Add(new Temple.Domain.Chat.ChatChannel { TenantId = firstTenant.Id, Key = "announcements", Name = "Announcements", IsSystem = true });
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded system chat channels for tenant {TenantId}", firstTenant.Id);
        }
        async Task EnsureUser(string email, string password, string roleKey)
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            Guid userId;
            if (existing != null)
            {
                userId = existing.Id;
            }
            else
            {
                var user = new User { Email = email, TenantId = firstTenant.Id };
                user.PasswordHash = hasher.HashPassword(user, password);
                db.Users.Add(user);
                await db.SaveChangesAsync(cancellationToken);
                userId = user.Id;
                _logger.LogInformation("Seeded user {Email}", email);
            }
            var tenantUser = await db.TenantUsers.FirstOrDefaultAsync(t => t.UserId == userId && t.TenantId == firstTenant.Id, cancellationToken);
            if (tenantUser == null)
            {
                db.TenantUsers.Add(new TenantUser { TenantId = firstTenant.Id, UserId = userId, RoleKey = roleKey });
                await db.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Assigned role {Role} to {Email}", roleKey, email);
            }
        }
        await EnsureUser("admin@admin.com", "Admin#123", RoleCapabilities.TenantOwner);
        await EnsureUser("joe@joe.com", "P@ssword1", RoleCapabilities.Member);
        // Promote first seeded admin to super admin if not already
        var super = await db.Users.FirstOrDefaultAsync(u => u.Email == "admin@admin.com", cancellationToken);
        if (super != null && !super.IsSuperAdmin)
        {
            super.IsSuperAdmin = true;
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Promoted {Email} to super admin", super.Email);
        }
    }

    private async Task SeedTaxonomyAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.ReligionTaxonomies.AnyAsync(ct)) return; // already seeded
        try
        {
            var rootPath = AppContext.BaseDirectory;
            // Walk up to find 'seed' directory (since base dir is bin/Debug/net8.0)
            string? FindSeedDir()
            {
                var dir = new DirectoryInfo(rootPath);
                for (int i = 0; i < 6 && dir != null; i++)
                {
                    var candidate = Path.Combine(dir.FullName, "seed", "religion-taxonomy.json");
                    if (File.Exists(candidate)) return candidate;
                    dir = dir.Parent;
                }
                return null;
            }
            var file = FindSeedDir();
            if (file == null)
            {
                _logger.LogWarning("religion-taxonomy.json not found; skipping taxonomy seed");
                return;
            }
            var json = await File.ReadAllTextAsync(file, ct);
            using var doc = System.Text.Json.JsonDocument.Parse(json);

            var list = new List<Temple.Domain.Taxonomy.ReligionTaxonomy>();
            void Recurse(System.Text.Json.JsonElement element, string? parentId)
            {
                var id = element.GetProperty("id").GetString() ?? string.Empty;
                var type = element.GetProperty("type").GetString() ?? string.Empty;
                var displayName = element.GetProperty("displayName").GetString() ?? string.Empty;
                string[] canonicalTexts = Array.Empty<string>();
                if (element.TryGetProperty("canonicalTexts", out var textsProp) && textsProp.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    canonicalTexts = textsProp.EnumerateArray().Select(t => t.GetString() ?? string.Empty).Where(s => s.Length > 0).ToArray();
                }
                string? terminologyJson = null;
                if (element.TryGetProperty("defaultTerminology", out var termProp))
                {
                    terminologyJson = termProp.GetRawText();
                }
                list.Add(new Temple.Domain.Taxonomy.ReligionTaxonomy
                {
                    Id = id,
                    ParentId = parentId,
                    Type = type,
                    DisplayName = displayName,
                    CanonicalTexts = canonicalTexts,
                    DefaultTerminologyJson = terminologyJson
                });
                if (element.TryGetProperty("children", out var children) && children.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var child in children.EnumerateArray())
                    {
                        Recurse(child, id);
                    }
                }
            }
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                Recurse(item, null);
            }
            db.ReligionTaxonomies.AddRange(list);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Seeded {Count} taxonomy nodes", list.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed seeding taxonomy");
        }
    }

    private async Task SeedDailyContentAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.DailyContents.AnyAsync(ct)) return;
        // Simple seed: one per sample taxonomy sect if exists
        var sects = await db.ReligionTaxonomies.Where(t => t.Type == "sect").Select(t => t.Id).ToListAsync(ct);
        foreach (var s in sects)
        {
            db.DailyContents.Add(new Temple.Domain.Content.DailyContent { TaxonomyId = s, Body = $"Welcome reflection for {s}" });
        }
        await db.SaveChangesAsync(ct);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
