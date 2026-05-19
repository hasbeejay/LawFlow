using LawFlow.Data;
using LawFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LawFlow.Services
{
    public class MessageService
    {
        private readonly ApplicationDbContext _context;

        public static event Action<int, Message>? OnMessageSent;

        public MessageService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Message>> GetMessagesForCaseAsync(int caseId)
        {
            return await _context.Messages
                .Where(m => m.CaseId == caseId)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<Message> SaveMessageAsync(int caseId, string senderId, string senderName, string senderRole, string content)
        {
            var message = new Message
            {
                CaseId = caseId,
                SenderId = senderId,
                SenderName = senderName,
                SenderRole = senderRole,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            
            // Dispatch live C# event to all active Blazor circuits
            try
            {
                OnMessageSent?.Invoke(caseId, message);
            }
            catch {}

            return message;
        }
    }
}
