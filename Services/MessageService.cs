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

        // Single source of truth: which two-party channel a given role belongs to.
        // A role NOT in this map cannot participate in chat.
        public static ChatChannel? GetChannelForRole(string? role) => role switch
        {
            "Client" => ChatChannel.ClientLawyer,
            "Lawyer" => ChatChannel.ClientLawyer,
            "Judge"  => ChatChannel.JudgeClerk,
            "Clerk"  => ChatChannel.JudgeClerk,
            "Admin"  => ChatChannel.AdminPolice,
            "Police" => ChatChannel.AdminPolice,
            _ => null
        };

        // Verify the user is an assigned party on the case AND their role's channel
        // is one of the three allowed pairs. Admin bypasses participation only for
        // their own channel (AdminPolice) — they cannot read Client↔Lawyer chats.
        public async Task<bool> CanUserAccessCaseChatAsync(string userId, string role, int caseId)
        {
            var channel = GetChannelForRole(role);
            if (channel == null) return false;

            var c = await _context.Cases.FirstOrDefaultAsync(x => x.Id == caseId);
            if (c == null) return false;

            return role switch
            {
                "Client" => c.ClientId == userId,
                "Lawyer" => c.LawyerId == userId,
                "Judge"  => c.JudgeId  == userId,
                "Clerk"  => c.ClerkId  == userId,
                "Police" => c.PoliceId == userId,
                "Admin"  => true, // Admin sees all AdminPolice channels system-wide
                _ => false
            };
        }

        // Returns messages on this case ONLY for the user's allowed channel, and
        // only if the user is actually a participant. Anything else → empty list.
        public async Task<List<Message>> GetMessagesForCaseAsync(int caseId, string userId, string role)
        {
            if (!await CanUserAccessCaseChatAsync(userId, role, caseId))
                return new List<Message>();

            var channel = GetChannelForRole(role)!.Value;

            return await _context.Messages
                .Where(m => m.CaseId == caseId && m.Channel == channel)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        // Server-side authoritative save. The caller provides senderId+role from
        // their authenticated session; this method derives the Channel itself so
        // a tampered client cannot smuggle a message into another pair's channel.
        public async Task<Message?> SaveMessageAsync(int caseId, string senderId, string senderName, string senderRole, string content)
        {
            if (string.IsNullOrWhiteSpace(content)) return null;
            if (!await CanUserAccessCaseChatAsync(senderId, senderRole, caseId)) return null;

            var channel = GetChannelForRole(senderRole)!.Value;

            var message = new Message
            {
                CaseId = caseId,
                SenderId = senderId,
                SenderName = senderName,
                SenderRole = senderRole,
                Content = content,
                Channel = channel,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            try { OnMessageSent?.Invoke(caseId, message); } catch { }

            return message;
        }

        // List cases this user can chat on — used by the sidebar Chat picker.
        public async Task<List<Case>> GetCasesForChatAsync(string userId, string role)
        {
            if (GetChannelForRole(role) == null) return new List<Case>();

            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .Include(c => c.Clerk)
                .Include(c => c.Police)
                .AsQueryable();

            query = role switch
            {
                "Client" => query.Where(c => c.ClientId == userId),
                "Lawyer" => query.Where(c => c.LawyerId == userId),
                "Judge"  => query.Where(c => c.JudgeId  == userId),
                "Clerk"  => query.Where(c => c.ClerkId  == userId),
                "Police" => query.Where(c => c.PoliceId == userId),
                "Admin"  => query, // Admin can chat on every case (their channel only)
                _ => query.Where(c => false)
            };

            return await query.OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt).ToListAsync();
        }
    }
}
