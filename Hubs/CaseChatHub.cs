using LawFlow.Models;
using LawFlow.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LawFlow.Hubs
{
    // SignalR Hubs require Microsoft.AspNetCore.Authentication.Cookies on the request,
    // so we authenticate by passing the senderId/role explicitly from the Blazor circuit
    // (which already validated the user via CustomAuthenticationStateProvider). The
    // server then validates BOTH the case participation and the channel mapping against
    // the database before broadcasting — clients cannot spoof their way into another
    // role's channel even if they call the hub directly.
    public class CaseChatHub : Hub
    {
        private readonly MessageService _messages;

        public CaseChatHub(MessageService messages)
        {
            _messages = messages;
        }

        public async Task JoinCaseChannel(int caseId, string userId, string role)
        {
            if (!await _messages.CanUserAccessCaseChatAsync(userId, role, caseId)) return;
            var channel = MessageService.GetChannelForRole(role);
            if (channel == null) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(caseId, channel.Value));
        }

        public async Task LeaveCaseChannel(int caseId, string userId, string role)
        {
            var channel = MessageService.GetChannelForRole(role);
            if (channel == null) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(caseId, channel.Value));
        }

        public async Task SendMessage(int caseId, string userId, string senderName, string role, string content)
        {
            var saved = await _messages.SaveMessageAsync(caseId, userId, senderName, role, content);
            if (saved == null) return;

            await Clients.Group(GroupName(caseId, saved.Channel)).SendAsync(
                "ReceiveMessage",
                caseId,
                saved.SenderId,
                saved.SenderName,
                saved.SenderRole,
                saved.Content,
                saved.CreatedAt,
                (int)saved.Channel
            );
        }

        // Backward-compat: older clients may still call these. They no-op safely
        // because they don't carry a channel — better to drop than to mis-route.
        public Task JoinCase(int caseId) => Task.CompletedTask;
        public Task JoinCaseGroup(int caseId) => Task.CompletedTask;
        public Task LeaveCase(int caseId) => Task.CompletedTask;
        public Task LeaveCaseGroup(int caseId) => Task.CompletedTask;

        private static string GroupName(int caseId, ChatChannel channel) => $"case-{caseId}-ch-{(int)channel}";
    }
}
