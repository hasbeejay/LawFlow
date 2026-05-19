using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace LawFlow.Hubs
{
    public class CaseChatHub : Hub
    {
        public async Task JoinCase(int caseId)
        {
            var groupName = GetGroupName(caseId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task JoinCaseGroup(int caseId)
        {
            await JoinCase(caseId);
        }

        public async Task LeaveCase(int caseId)
        {
            var groupName = GetGroupName(caseId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveCaseGroup(int caseId)
        {
            await LeaveCase(caseId);
        }

        public async Task SendMessage(int caseId, string senderId, string senderName, string senderRole, string content)
        {
            var groupName = GetGroupName(caseId);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", caseId, senderId, senderName, senderRole, content, DateTime.UtcNow);
        }

        private string GetGroupName(int caseId) => $"case-{caseId}";
    }
}
