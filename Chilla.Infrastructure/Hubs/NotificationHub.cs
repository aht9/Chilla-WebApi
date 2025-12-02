using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Chilla.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // متدهایی برای ارسال اعلان به کلاینت
    public async Task SendPersonalNotification(string userId, string message)
    {
        await Clients.User(userId).SendAsync("ReceiveMessage", message);
    }
}