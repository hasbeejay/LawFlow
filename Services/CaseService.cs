using LawFlow.Data;
using LawFlow.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LawFlow.Services
{
    public class CaseService
    {
        private readonly ApplicationDbContext _context;

        public CaseService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all cases
        public async Task<List<Case>> GetAllCasesAsync()
        {
            return await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .Include(c => c.Police)
                .Include(c => c.Clerk)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Get cases filtered by role
        public async Task<List<Case>> GetCasesForUserAsync(string userId, UserRole role)
        {
            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .Include(c => c.Police)
                .Include(c => c.Clerk)
                .AsQueryable();

            switch (role)
            {
                case UserRole.Client:
                    query = query.Where(c => c.ClientId == userId);
                    break;
                case UserRole.Lawyer:
                    // Show cases assigned OR cases available for lawyers to accept (AvailableForLawyers status)
                    query = query.Where(c => c.LawyerId == userId || (c.Status == CaseStatus.AvailableForLawyers && c.LawyerId == null));
                    break;
                case UserRole.Judge:
                    query = query.Where(c => c.JudgeId == userId);
                    break;
                case UserRole.Police:
                    query = query.Where(c => c.PoliceId == userId);
                    break;
                case UserRole.Clerk:
                    query = query.Where(c => c.ClerkId == userId);
                    break;
                case UserRole.Admin:
                    // Admin sees all
                    break;
            }

            return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        public async Task<Case?> GetCaseByIdAsync(int id)
        {
            return await _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .Include(c => c.Police)
                .Include(c => c.Clerk)
                .Include(c => c.Hearings)
                .Include(c => c.Documents)
                .Include(c => c.PoliceReports)
                .Include(c => c.Verdict)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Search cases
        public async Task<List<Case>> SearchCasesAsync(string? queryText, CaseStatus? status, string? lawyerId, string? judgeId)
        {
            var q = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .AsQueryable();

            if (status.HasValue)
            {
                q = q.Where(c => c.Status == status.Value);
            }

            if (!string.IsNullOrEmpty(lawyerId))
            {
                q = q.Where(c => c.LawyerId == lawyerId);
            }

            if (!string.IsNullOrEmpty(judgeId))
            {
                q = q.Where(c => c.JudgeId == judgeId);
            }

            if (!string.IsNullOrEmpty(queryText))
            {
                queryText = queryText.ToLower();
                q = q.Where(c => c.CaseNumber.ToLower().Contains(queryText) ||
                               c.Title.ToLower().Contains(queryText) ||
                               c.Description.ToLower().Contains(queryText) ||
                               (c.Client != null && c.Client.FullName.ToLower().Contains(queryText)));
            }

            return await q.OrderByDescending(c => c.CreatedAt).ToListAsync();
        }

        // Validate state transitions
        public bool ValidateTransition(CaseStatus current, CaseStatus next)
        {
            return (current, next) switch
            {
                (CaseStatus.Created, CaseStatus.ReviewedByAdmin) => true,
                (CaseStatus.Created, CaseStatus.Closed) => true, // Admin rejects FIR
                (CaseStatus.ReviewedByAdmin, CaseStatus.AssignedToJudgeAndPolice) => true,
                (CaseStatus.AssignedToJudgeAndPolice, CaseStatus.AvailableForLawyers) => true,
                (CaseStatus.AvailableForLawyers, CaseStatus.LawyerAccepted) => true,
                (CaseStatus.LawyerAccepted, CaseStatus.AssignedToLawyer) => true,
                (CaseStatus.AssignedToLawyer, CaseStatus.ClerkAssignedByJudge) => true,
                (CaseStatus.ClerkAssignedByJudge, CaseStatus.Investigation) => true,
                (CaseStatus.Investigation, CaseStatus.Hearing) => true,
                (CaseStatus.Hearing, CaseStatus.VerdictIssued) => true,
                (CaseStatus.VerdictIssued, CaseStatus.Closed) => true,
                
                // Flexible direct flows for admin reassignment or fallback compatibility
                (CaseStatus.AssignedToLawyer, CaseStatus.Investigation) => true,
                (CaseStatus.ReviewedByAdmin, CaseStatus.AvailableForLawyers) => true,
                _ => false
            };
        }

        // Transition case status programmatically
        public async Task<bool> TransitionCaseStatusAsync(int caseId, CaseStatus newStatus, string actorId)
        {
            var c = await _context.Cases.FindAsync(caseId);
            if (c == null) return false;

            if (!ValidateTransition(c.Status, newStatus))
            {
                return false;
            }

            c.Status = newStatus;
            c.UpdatedAt = DateTime.UtcNow;

            await LogActivityAsync(actorId, "Case Transitioned", $"Transitioned case {c.CaseNumber} status to {newStatus}");
            await _context.SaveChangesAsync();
            return true;
        }

        // 1. Client submits Case (FIR) -> Status = Created
        public async Task<Case> SubmitFIRAsync(string title, string description, string clientId, string initialFileName, string initialFilePath)
        {
            var year = DateTime.UtcNow.Year;
            var count = await _context.Cases.CountAsync(c => c.CreatedAt.Year == year) + 1;
            var caseNumber = $"LF-{year}-{count:D4}";

            var newCase = new Case
            {
                CaseNumber = caseNumber,
                Title = title,
                Description = description,
                ClientId = clientId,
                Status = CaseStatus.Created,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Cases.Add(newCase);
            await _context.SaveChangesAsync();

            // Add FIR document
            if (!string.IsNullOrEmpty(initialFileName))
            {
                var doc = new Document
                {
                    CaseId = newCase.Id,
                    FileName = initialFileName,
                    FilePath = initialFilePath,
                    DocumentType = "FIR",
                    UploadedById = clientId,
                    UploadedAt = DateTime.UtcNow,
                    IsApproved = true
                };
                _context.Documents.Add(doc);
            }

            // Log activity
            await LogActivityAsync(clientId, "FIR Lodged", $"Lodged new judicial complaint {caseNumber} - {title}");

            // Notify Admin
            await SendNotificationToRoleAsync(UserRole.Admin, "New FIR Lodged", $"A new FIR {caseNumber} has been filed and requires review.");

            await _context.SaveChangesAsync();
            return newCase;
        }

        // 2. Admin reviews and approves FIR -> Sets to ReviewedByAdmin (or Closed if rejected)
        public async Task<bool> ReviewFIRAsync(int caseId, string adminId, bool approve)
        {
            var c = await _context.Cases.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == caseId);
            if (c == null) return false;

            if (approve)
            {
                if (c.Status != CaseStatus.Created) return false;

                c.Status = CaseStatus.ReviewedByAdmin;
                c.UpdatedAt = DateTime.UtcNow;
                await LogActivityAsync(adminId, "FIR Approved", $"Approved FIR {c.CaseNumber}");
                await SendNotificationAsync(c.ClientId, "FIR Approved", $"Your FIR {c.CaseNumber} has been approved by the Administrator and is undergoing court assignments.");
            }
            else
            {
                c.Status = CaseStatus.Closed;
                c.UpdatedAt = DateTime.UtcNow;
                await LogActivityAsync(adminId, "FIR Rejected", $"Rejected and Closed FIR {c.CaseNumber}");
                await SendNotificationAsync(c.ClientId, "FIR Rejected", $"Your FIR {c.CaseNumber} was rejected after review.");
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // 3. Lawyer accepts/declines case
        public async Task<bool> UpdateLawyerAssignmentAsync(int caseId, string lawyerId, bool accept)
        {
            var c = await _context.Cases.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == caseId);
            if (c == null) return false;

            if (accept)
            {
                if (c.Status != CaseStatus.AvailableForLawyers) return false;

                c.LawyerId = lawyerId;
                
                // Move sequentially through LawyerAccepted -> AssignedToLawyer
                c.Status = CaseStatus.LawyerAccepted;
                c.UpdatedAt = DateTime.UtcNow;
                await LogActivityAsync(lawyerId, "Lawyer Accepted Case", $"Lawyer accepted case {c.CaseNumber}");
                await _context.SaveChangesAsync();

                c.Status = CaseStatus.AssignedToLawyer;
                c.UpdatedAt = DateTime.UtcNow;

                var lawyer = await _context.Users.FindAsync(lawyerId);
                var lawyerName = lawyer?.FullName ?? "A Lawyer";

                await SendNotificationAsync(c.ClientId, "Lawyer Assigned", $"{lawyerName} has accepted your case.");
                await SendNotificationToRoleAsync(UserRole.Admin, "Lawyer Assigned", $"Lawyer {lawyerName} accepted case {c.CaseNumber}. Needs Judge, Police, and Clerk assignments.");
            }
            else
            {
                // Re-open case to other lawyers
                c.LawyerId = null;
                c.Status = CaseStatus.AvailableForLawyers;
                c.UpdatedAt = DateTime.UtcNow;
                await LogActivityAsync(lawyerId, "Lawyer Declined Case", $"Lawyer declined case {c.CaseNumber}");
            }

            await _context.SaveChangesAsync();
            return true;
        }

        // 4. Admin assigns remaining parties (Judge, Police, Clerk) -> Status goes to AssignedToJudgeAndPolice -> AvailableForLawyers -> ... -> Investigation
        public async Task<bool> AssignCoreOfficialsAsync(int caseId, string adminId, string judgeId, string policeId, string clerkId)
        {
            var c = await _context.Cases.Include(c => c.Client).FirstOrDefaultAsync(c => c.Id == caseId);
            if (c == null) return false;

            c.JudgeId = judgeId;
            c.PoliceId = policeId;
            c.ClerkId = clerkId;

            // Follow 11-step machine flow:
            // ReviewedByAdmin -> AssignedToJudgeAndPolice -> AvailableForLawyers
            if (c.Status == CaseStatus.ReviewedByAdmin)
            {
                c.Status = CaseStatus.AssignedToJudgeAndPolice;
                c.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                c.Status = CaseStatus.AvailableForLawyers;
                c.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                // Notify all lawyers that a new case is available
                await SendNotificationToRoleAsync(UserRole.Lawyer, "Case Available", $"Case {c.CaseNumber} is open and awaiting a lawyer.");
            }
            else if (c.Status == CaseStatus.AssignedToLawyer || c.Status == CaseStatus.LawyerAccepted)
            {
                // If lawyer is already assigned, advance directly to ClerkAssignedByJudge then Investigation
                c.Status = CaseStatus.ClerkAssignedByJudge;
                c.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                c.Status = CaseStatus.Investigation;
                c.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            else
            {
                // General fallback
                c.Status = CaseStatus.Investigation;
                c.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            await LogActivityAsync(adminId, "Officials Assigned", $"Assigned Judge, Police, and Clerk to case {c.CaseNumber}");
            
            // Notifications
            await SendNotificationAsync(c.ClientId, "Investigation Started", $"Officials have been assigned to your case. Investigation is now active.");
            if (c.LawyerId != null)
                await SendNotificationAsync(c.LawyerId, "Officials Assigned", $"Case officials assigned for case {c.CaseNumber}.");
            
            await SendNotificationAsync(judgeId, "Case Assigned", $"You have been assigned to preside over case {c.CaseNumber}.");
            await SendNotificationAsync(policeId, "Investigation Assigned", $"You have been assigned as lead investigator for case {c.CaseNumber}.");
            await SendNotificationAsync(clerkId, "Case Scheduled Coordination", $"You have been assigned as Clerk for case {c.CaseNumber}.");

            await _context.SaveChangesAsync();
            return true;
        }

        // Helper logging
        public async Task LogActivityAsync(string userId, string action, string details)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(log);
        }

        public async Task SendNotificationAsync(string userId, string title, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);
        }

        public async Task SendNotificationToRoleAsync(UserRole role, string title, string message)
        {
            var users = await _context.Users.Where(u => u.Role == role && u.IsActive).ToListAsync();
            foreach (var user in users)
            {
                await SendNotificationAsync(user.Id, title, message);
            }
        }

        // Seed comprehensive historical dashboard data matching the 11-stage status machine
        public async Task SeedDemoCasesAsync()
        {
            // Ensure a clean slate for cases and related data
            _context.Cases.RemoveRange(_context.Cases);
            _context.Hearings.RemoveRange(_context.Hearings);
            _context.Documents.RemoveRange(_context.Documents);
            _context.PoliceReports.RemoveRange(_context.PoliceReports);
            _context.Verdicts.RemoveRange(_context.Verdicts);
            await _context.SaveChangesAsync();

            // Retrieve seeded users by username
            var clientZaheer = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "client_zaheer");
            var clientFariha = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "client_fariha");
            var clientHaneef = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "client_haneef");
            var clientSamira = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "client_samira");

            var lawyerFaisal = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "lawyer_faisal");
            var lawyerNadia = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "lawyer_nadia");
            var lawyerUzair = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "lawyer_uzair");

            var judgeAli = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "judge_ali");
            var judgeSaba = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "judge_saba");

            var policeRahim = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "police_rahim");
            var policeLara = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "police_lara");
            var policeHammad = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "police_hammad");
            var policeFarooq = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "police_farooq");

            var clerkAhmad = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "clerk_ahmad");
            var clerkLaila = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "clerk_laila");
            var clerkOsama = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "clerk_osama");
            var clerkFahad = await _context.Users.FirstOrDefaultAsync(u => u.UserName == "clerk_fahad");

            // Helper to safely get Ids
            string IdOr(string? id) => id ?? string.Empty;

            // 1. Created (Complaint Lodged)
            var case1 = new Case
            {
                CaseNumber = "PK-2026-0001",
                Title = "Illegal Construction of Unauthorized Mosque",
                Description = "A residential area reports an unauthorized mosque built without permits, causing community disputes.",
                Status = CaseStatus.Created,
                ClientId = IdOr(clientZaheer?.Id),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-20)
            };

            // 2. Investigation Case
            var case2 = new Case
            {
                CaseNumber = "PK-2026-0002",
                Title = "High‑Value Art Theft from Lahore Museum",
                Description = "Stolen paintings valued at PKR 150M reported missing; investigators suspect an inside job.",
                Status = CaseStatus.Investigation,
                ClientId = IdOr(clientFariha?.Id),
                LawyerId = IdOr(lawyerFaisal?.Id),
                JudgeId = IdOr(judgeAli?.Id),
                PoliceId = IdOr(policeRahim?.Id),
                ClerkId = IdOr(clerkAhmad?.Id),
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            // 3. Hearing Case
            var case3 = new Case
            {
                CaseNumber = "PK-2026-0003",
                Title = "Corporate Fraud at National Bank",
                Description = "Allegations of embezzlement and false accounting spanning three fiscal years.",
                Status = CaseStatus.Hearing,
                ClientId = IdOr(clientHaneef?.Id),
                LawyerId = IdOr(lawyerNadia?.Id),
                JudgeId = IdOr(judgeSaba?.Id),
                PoliceId = IdOr(policeLara?.Id),
                ClerkId = IdOr(clerkLaila?.Id),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-5)
            };

            // 4. VerdictIssued Case
            var case4 = new Case
            {
                CaseNumber = "PK-2026-0004",
                Title = "Environmental Pollution Violation by Textile Mill",
                Description = "Factory accused of dumping hazardous waste into the River Ravi, violating EPA standards.",
                Status = CaseStatus.VerdictIssued,
                ClientId = IdOr(clientSamira?.Id),
                LawyerId = IdOr(lawyerUzair?.Id),
                JudgeId = IdOr(judgeAli?.Id),
                PoliceId = IdOr(policeHammad?.Id),
                ClerkId = IdOr(clerkOsama?.Id),
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            // 5. Closed Case with Verdict
            var case5 = new Case
            {
                CaseNumber = "PK-2026-0005",
                Title = "Kidnapping Ring Dismantled in Karachi",
                Description = "Multi‑city kidnapping operation uncovered; suspects arrested and charged.",
                Status = CaseStatus.Closed,
                ClientId = IdOr(clientZaheer?.Id),
                LawyerId = IdOr(lawyerFaisal?.Id),
                JudgeId = IdOr(judgeSaba?.Id),
                PoliceId = IdOr(policeFarooq?.Id),
                ClerkId = IdOr(clerkFahad?.Id),
                CreatedAt = DateTime.UtcNow.AddDays(-45),
                UpdatedAt = DateTime.UtcNow.AddDays(-10)
            };

            // Add cases to context
            _context.Cases.AddRange(case1, case2, case3, case4, case5);
            await _context.SaveChangesAsync();

            // Seed supporting data
            var hearing = new Hearing
            {
                CaseId = case3.Id,
                HearingDate = DateTime.UtcNow.AddDays(3),
                Location = "Lahore High Courtroom 2",
                Notes = "Final arguments and cross‑examination.",
                Status = "Scheduled",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            _context.Hearings.Add(hearing);

            var verdict = new Verdict
            {
                CaseId = case5.Id,
                JudgeId = case5.JudgeId,
                Type = VerdictType.Guilty,
                Details = "All accused found guilty of kidnapping and sentenced to 20 years imprisonment.",
                IsPublished = true,
                IssuedAt = DateTime.UtcNow.AddDays(-12),
                PublishedAt = DateTime.UtcNow.AddDays(-12)
            };
            _context.Verdicts.Add(verdict);

            var policeReport = new PoliceReport
            {
                CaseId = case2.Id,
                OfficerId = case2.PoliceId,
                Summary = "Recovered stolen artworks from a hidden warehouse; forensic analysis linked suspects.",
                CriminalRecordUpdated = true,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };
            _context.PoliceReports.Add(policeReport);

            var doc1 = new Document
            {
                CaseId = case2.Id,
                FileName = "Incident_Report.pdf",
                FilePath = "/uploads/incident_report.pdf",
                DocumentType = "Report",
                UploadedById = case2.PoliceId,
                UploadedAt = DateTime.UtcNow.AddDays(-14),
                IsApproved = true
            };
            var doc2 = new Document
            {
                CaseId = case2.Id,
                FileName = "Surveillance_Video.mp4",
                FilePath = "/uploads/surveillance.mp4",
                DocumentType = "Evidence",
                UploadedById = case2.PoliceId,
                UploadedAt = DateTime.UtcNow.AddDays(-13),
                IsApproved = true
            };
            _context.Documents.AddRange(doc1, doc2);

            await _context.SaveChangesAsync();
        }
    }
}
