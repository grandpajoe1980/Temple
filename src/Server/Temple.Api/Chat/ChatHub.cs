using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Temple.Infrastructure.Persistence;
using Temple.Domain.Chat;
using Temple.Domain.Identity;
using Temple.Api.Middleware;

namespace Temple.Api.Chat;

[Authorize]
public class ChatHub : Hub
{
    private readonly AppDbContext _db;
    private readonly TenantContext _tenant;
    public ChatHub(AppDbContext db, TenantContext tenant) { _db = db; _tenant = tenant; }

    public async Task SendMessage(string channelKey, string message, string? type = null)
    {
        if (_tenant.TenantId == null) throw new HubException("No tenant");
        var userIdStr = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) throw new HubException("Invalid user");
        var channel = _db.ChatChannels.FirstOrDefault(c => c.TenantId == _tenant.TenantId && c.Key == channelKey);
        if (channel == null)
        {
            // auto-create general channel; restrict others for now
            if (!string.Equals(channelKey, "general", StringComparison.OrdinalIgnoreCase))
                throw new HubException("Unknown channel");
            channel = new ChatChannel { TenantId = _tenant.TenantId.Value, Key = channelKey.ToLowerInvariant(), Name = "General", IsSystem = true };
            _db.ChatChannels.Add(channel);
            await _db.SaveChangesAsync();
        }
        // Private channel membership check
        if (channel.IsPrivate)
        {
            var member = _db.ChatChannelMembers.FirstOrDefault(m => m.TenantId == _tenant.TenantId && m.ChannelId == channel.Id && m.UserId == userId);
            if (member == null) throw new HubException("Not a member");
        }
        var caps = Context.User?.Claims.Where(c => c.Type == "cap").Select(c => c.Value).ToHashSet() ?? new();
        if (string.Equals(channelKey, "announcements", StringComparison.OrdinalIgnoreCase) && !caps.Contains(Capability.ChatPostAnnouncement))
            throw new HubException("Not allowed");
        var msg = new ChatMessage { TenantId = _tenant.TenantId.Value, ChannelId = channel.Id, UserId = userId, Body = message, Type = type ?? (channel.Key == "announcements" ? "announcement" : "standard") };
        _db.ChatMessages.Add(msg);
        await _db.SaveChangesAsync();
        await Clients.Group(channel.Id.ToString()).SendAsync("message", new { id = msg.Id, channel = channel.Key, body = msg.Body, userId = msg.UserId, createdUtc = msg.CreatedUtc, type = msg.Type });
    }

    public async Task Typing(string channelKey)
    {
        if (_tenant.TenantId == null) return;
        var userIdStr = Context.User?.FindFirst("sub")?.Value;
        if (!Guid.TryParse(userIdStr, out var userId)) return;
        var channel = _db.ChatChannels.FirstOrDefault(c => c.TenantId == _tenant.TenantId && c.Key == channelKey);
        if (channel == null) return;
        await Clients.Group(channel.Id.ToString()).SendAsync("typing", new { channel = channel.Key, userId, at = DateTime.UtcNow });
    }

    public override async Task OnConnectedAsync()
    {
        if (_tenant.TenantId != null)
        {
            // join all system channels automatically
            var channels = _db.ChatChannels.Where(c => c.TenantId == _tenant.TenantId && (c.IsSystem || c.Key == "general"));
            foreach (var ch in channels)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, ch.Id.ToString());
            }
            var userIdStr = Context.User?.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                // Upsert presence
                var presence = new ChatPresence { TenantId = _tenant.TenantId.Value, UserId = userId, ConnectionId = Context.ConnectionId, ConnectedUtc = DateTime.UtcNow, LastActiveUtc = DateTime.UtcNow };
                _db.ChatPresences.Add(presence);
                await _db.SaveChangesAsync();
                await Clients.Group("presence:" + _tenant.TenantId).SendAsync("presence.join", new { userId, connectedUtc = presence.ConnectedUtc });
                await Groups.AddToGroupAsync(Context.ConnectionId, "presence:" + _tenant.TenantId);
            }
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (_tenant.TenantId != null)
        {
            var userIdStr = Context.User?.FindFirst("sub")?.Value;
            if (Guid.TryParse(userIdStr, out var userId))
            {
                var pres = _db.ChatPresences.FirstOrDefault(p => p.TenantId == _tenant.TenantId && p.ConnectionId == Context.ConnectionId);
                if (pres != null)
                {
                    pres.DisconnectedUtc = DateTime.UtcNow;
                    pres.LastActiveUtc = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    await Clients.Group("presence:" + _tenant.TenantId).SendAsync("presence.leave", new { userId, disconnectedUtc = pres.DisconnectedUtc });
                }
            }
        }
        await base.OnDisconnectedAsync(exception);
    }
}
