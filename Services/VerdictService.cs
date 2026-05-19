using LawFlow.Data;
using LawFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LawFlow.Services
{
    public class VerdictService
    {
        private readonly ApplicationDbContext _context;

        public VerdictService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> IssueVerdictAsync(int caseId, string judgeId, VerdictType type, string details)
        {
            var c = await _context.Cases.Include(x => x.Verdict).FirstOrDefaultAsync(x => x.Id == caseId);
            if (c == null) return false;

            if (c.Verdict != null)
            {
                // Update existing
                c.Verdict.Type = type;
                c.Verdict.Details = details;
                c.Verdict.IssuedAt = DateTime.UtcNow;
            }
            else
            {
                var verdict = new Verdict
                {
                    CaseId = caseId,
                    JudgeId = judgeId,
                    Type = type,
                    Details = details,
                    IsPublished = false,
                    IssuedAt = DateTime.UtcNow
                };
                _context.Verdicts.Add(verdict);
            }

            c.Status = CaseStatus.VerdictIssued;
            c.UpdatedAt = DateTime.UtcNow;

            // Log
            var log = new ActivityLog
            {
                UserId = judgeId,
                Action = "Verdict Issued",
                Details = $"Presiding Judge issued {type} verdict on case {c.CaseNumber}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            // Notify Clerk to publish
            if (!string.IsNullOrEmpty(c.ClerkId))
            {
                var notif = new Notification
                {
                    UserId = c.ClerkId,
                    Title = "Verdict Needs Publishing",
                    Message = $"A verdict has been issued for case {c.CaseNumber} and is ready for publication.",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notif);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> PublishVerdictAsync(int caseId, string clerkId)
        {
            var c = await _context.Cases
                .Include(c => c.Verdict)
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Police)
                .FirstOrDefaultAsync(c => c.Id == caseId);

            if (c?.Verdict == null) return false;

            c.Verdict.IsPublished = true;
            c.Verdict.PublishedAt = DateTime.UtcNow;
            c.Status = CaseStatus.Closed;
            c.UpdatedAt = DateTime.UtcNow;

            // Log
            var log = new ActivityLog
            {
                UserId = clerkId,
                Action = "Verdict Published",
                Details = $"Clerk published verdict '{c.Verdict.Type}' for case {c.CaseNumber}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            // Notify everyone
            var parties = new[] { c.ClientId, c.LawyerId, c.PoliceId };
            foreach (var p in parties)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    var notif = new Notification
                    {
                        UserId = p,
                        Title = "Verdict Published",
                        Message = $"The final verdict ({c.Verdict.Type}) for case {c.CaseNumber} has been published.",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notif);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
