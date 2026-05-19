using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LawFlow.Hubs
{
    /// <summary>
    /// Hub for generic user notifications (toast alerts, etc.).
    /// Case‑specific chat is now handled by CaseChatHub.
    /// </summary>
    public class NotificationHub : Hub
    {
        public async Task JoinUserGroup(string userId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);
        }

        public async Task LeaveUserGroup(string userId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
        }

        public async Task SendLiveNotification(string userId, string title, string message)
        {
            await Clients.Group(userId).SendAsync("ReceiveNotification", title, message);
        }
    }
}

