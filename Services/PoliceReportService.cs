using LawFlow.Data;
using LawFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LawFlow.Services
{
    public class PoliceReportService
    {
        private readonly ApplicationDbContext _context;

        public PoliceReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PoliceReport>> GetReportsForCaseAsync(int caseId)
        {
            return await _context.PoliceReports
                .Include(r => r.Officer)
                .Where(r => r.CaseId == caseId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> SubmitReportAsync(int caseId, string officerId, string summary, bool updateCriminalLog)
        {
            var c = await _context.Cases.FindAsync(caseId);
            if (c == null) return false;

            var report = new PoliceReport
            {
                CaseId = caseId,
                OfficerId = officerId,
                Summary = summary,
                CriminalRecordUpdated = updateCriminalLog,
                CreatedAt = DateTime.UtcNow
            };

            _context.PoliceReports.Add(report);

            // Log activity
            var log = new ActivityLog
            {
                UserId = officerId,
                Action = "Police Report Submitted",
                Details = $"Officer submitted investigation report for case {c.CaseNumber}. Criminal Record Updated: {updateCriminalLog}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);

            // Notify Judge and Lawyer
            var parties = new[] { c.JudgeId, c.LawyerId, c.ClientId };
            foreach (var p in parties)
            {
                if (!string.IsNullOrEmpty(p))
                {
                    var notif = new Notification
                    {
                        UserId = p,
                        Title = "Police Investigation Report Uploaded",
                        Message = $"Lead investigator has submitted a new report for case {c.CaseNumber}.",
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
