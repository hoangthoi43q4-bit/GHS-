using Microsoft.AspNetCore.SignalR;

namespace GHS.Web.Hubs;

/// <summary>
/// SignalR Hub for pushing real-time communication logs to connected Blazor clients.
/// </summary>
public class LogHub : Hub
{
    public async Task SubscribeToLogs()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "LogSubscribers");
    }
}
