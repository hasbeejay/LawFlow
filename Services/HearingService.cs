using LawFlow.Data;
using LawFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LawFlow.Services
{
    public class HearingService
    {
        private readonly ApplicationDbContext _context;

        public HearingService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Hearing>> GetAllHearingsAsync()
        {
            return await _context.Hearings
                .Include(h => h.Case)
                .OrderBy(h => h.HearingDate)
                .ToListAsync();
        }

        public async Task<List<Hearing>> GetHearingsForCaseAsync(int caseId)
        {
            return await _context.Hearings
                .Where(h => h.CaseId == caseId)
                .OrderBy(h => h.HearingDate)
                .ToListAsync();
        }

        public async Task<bool> ScheduleHearingAsync(int caseId, DateTime date, string location, string notes, string clerkId)
        {
            var c = await _context.Cases.FindAsync(caseId);
            if (c == null) return false;

            var hearing = new Hearing
            {
                CaseId = caseId,
                HearingDate = date,
                Location = location,
                Notes = notes,
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow
            };

            _context.Hearings.Add(hearing);

            // Also advance case status to Hearing if it's Investigation
            if (c.Status == CaseStatus.Investigation)
            {
                c.Status = CaseStatus.Hearing;
                c.UpdatedAt = DateTime.UtcNow;
            }

            // Log
            var log = new ActivityLog
            {
                UserId = clerkId,
                Action = "Hearing Scheduled",
                Details = $"Scheduled hearing for case {c.CaseNumber} on {date:g} at {location}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            // Notify parties
            var parties = new[] { c.ClientId, c.LawyerId, c.JudgeId, c.PoliceId };
            foreach (var p in parties)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    var notif = new Notification
                    {
                        UserId = p,
                        Title = "Hearing Scheduled",
                        Message = $"A hearing has been scheduled for case {c.CaseNumber} on {date:g} in {location}.",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(notif);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateHearingStatusAsync(int hearingId, string status, string notes, string clerkId)
        {
            var hearing = await _context.Hearings.Include(h => h.Case).FirstOrDefaultAsync(h => h.Id == hearingId);
            if (hearing == null) return false;

            hearing.Status = status;
            hearing.Notes = string.IsNullOrEmpty(notes) ? hearing.Notes : notes;

            // Log
            var log = new ActivityLog
            {
                UserId = clerkId,
                Action = "Hearing Updated",
                Details = $"Updated hearing status for case {hearing.Case?.CaseNumber} to {status}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
