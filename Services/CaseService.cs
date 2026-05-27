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
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        // Get cases for user with pagination
        public async Task<List<Case>> GetCasesPagedForUserAsync(string userId, UserRole role, int page, int pageSize)
        {
            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .Include(c => c.Police)
                .Include(c => c.Clerk)
                .AsNoTracking()
                .AsQueryable();

            switch (role)
            {
                case UserRole.Client:
                    query = query.Where(c => c.ClientId == userId);
                    break;
                case UserRole.Lawyer:
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

            return await query.OrderByDescending(c => c.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        // Get all cases for a user without pagination
        public async Task<List<Case>> GetCasesForUserAsync(string userId, UserRole role)
        {
            var query = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .Include(c => c.Police)
                .Include(c => c.Clerk)
                .AsNoTracking()
                .AsQueryable();

            switch (role)
            {
                case UserRole.Client:
                    query = query.Where(c => c.ClientId == userId);
                    break;
                case UserRole.Lawyer:
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
        
        // Get limited cases filtered by role for dashboards
        public async Task<List<Case>> GetRecentCasesForUserAsync(string userId, UserRole role, int limit)
        {
            var query = _context.Cases
                .Include(c => c.Client)
                .AsNoTracking()
                .AsQueryable();

            switch (role)
            {
                case UserRole.Client:
                    query = query.Where(c => c.ClientId == userId);
                    break;
                case UserRole.Lawyer:
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
                    break;
            }

            return await query.OrderByDescending(c => c.CreatedAt).Take(limit).ToListAsync();
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
                    .ThenInclude(d => d.UploadedBy)
                .Include(c => c.PoliceReports)
                    .ThenInclude(p => p.Officer)
                .Include(c => c.Verdict)
                    .ThenInclude(v => v!.Judge)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        // Search cases
        public async Task<List<Case>> SearchCasesAsync(string? queryText, CaseStatus? status, string? lawyerId, string? judgeId)
        {
            var q = _context.Cases
                .Include(c => c.Client)
                .Include(c => c.Lawyer)
                .Include(c => c.Judge)
                .AsNoTracking()
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

        // Client forwards a case to a specific lawyer. The case must be at a stage
        // where lawyer engagement makes sense and must belong to this client.
        public async Task<bool> ForwardCaseToLawyerAsync(int caseId, string clientId, string lawyerId)
        {
            var c = await _context.Cases.FirstOrDefaultAsync(x => x.Id == caseId);
            if (c == null) return false;
            if (c.ClientId != clientId) return false;
            if (c.Status is CaseStatus.Closed or CaseStatus.VerdictIssued) return false;

            // Promote to AvailableForLawyers if it isn't already past that point.
            if (c.Status == CaseStatus.Created || c.Status == CaseStatus.ReviewedByAdmin
                || c.Status == CaseStatus.AssignedToJudgeAndPolice)
            {
                c.Status = CaseStatus.AvailableForLawyers;
            }

            c.LawyerId = lawyerId;
            c.UpdatedAt = DateTime.UtcNow;

            await LogActivityAsync(clientId, "Lawyer Offer Sent", $"Client forwarded case {c.CaseNumber} to lawyer {lawyerId}");
            await SendNotificationAsync(lawyerId, "New Case Offered", $"You have been offered case {c.CaseNumber}. Open Offered Cases to accept or decline.");

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
                // Only the offered lawyer can accept. If no specific lawyer was offered
                // (admin published the case open), any lawyer claims it first.
                if (!string.IsNullOrEmpty(c.LawyerId) && c.LawyerId != lawyerId) return false;

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
            var users = await _context.Users.ToListAsync();
            var clients = users.Where(u => u.Role == UserRole.Client).ToList();
            var lawyers = users.Where(u => u.Role == UserRole.Lawyer).ToList();
            var judges = users.Where(u => u.Role == UserRole.Judge).ToList();
            var police = users.Where(u => u.Role == UserRole.Police).ToList();
            var clerks = users.Where(u => u.Role == UserRole.Clerk).ToList();

            if (!clients.Any()) return; // Sanity check

            var rand = new Random();
            Func<string[], string> pick = arr => arr[rand.Next(arr.Length)];
            var pakistaniCities = new[] { "Karachi", "Lahore", "Islamabad", "Rawalpindi", "Multan", "Peshawar", "Quetta", "Faisalabad", "Sialkot", "Hyderabad" };
            var pakistaniCourts = new[] { "Lahore High Court", "Islamabad District Court", "Karachi Sessions Court", "Peshawar District Court", "Multan Judicial Complex", "Quetta Judicial Complex" };
            var pakistaniPoliceStations = new[] { "Lahore Cantt Police Station", "Bahria Town Police Station", "Islamabad City Police Station", "Faisalabad Saddar Police Station", "Peshawar City Police Station", "Karachi Central Police Station", "Quetta Civil Lines Police Station" };
            var pakistaniCrimes = new[]
            {
                "Attempted robbery under Section 392 PPC",
                "Assault with intent under Section 324 PPC",
                "Fraudulent possession under Section 420 PPC",
                "Burglary at a residential compound under Section 457 PPC",
                "Dowry harassment complaint under Section 498-A PPC",
                "Hit-and-run injury under Section 279 PPC",
                "Illegal arms possession under the Arms Act",
                "Land dispute and trespass under Section 107 CrPC",
                "Cyber fraud under Section 420 PPC",
                "Domestic violence complaint under Section 354 PPC"
            };
            var pakistaniSentences = new[]
            {
                "two years imprisonment and a fine of PKR 50,000",
                "three years imprisonment with community service",
                "five years imprisonment under Section 302 PPC",
                "one year imprisonment and restitution to the victim",
                "a fine of PKR 100,000 and a formal apology",
                "six months imprisonment and probation",
                "seven years imprisonment with mandatory rehabilitation",
                "a suspended sentence with strict bail conditions"
            };
            var casesToInsert = new List<Case>();
            var docsToInsert = new List<Document>();
            var hearingsToInsert = new List<Hearing>();
            var reportsToInsert = new List<PoliceReport>();
            var verdictsToInsert = new List<Verdict>();

            var caseStatuses = Enum.GetValues(typeof(CaseStatus)).Cast<CaseStatus>().ToList();
            var earlyStatuses = caseStatuses.Take(8).ToArray();

            int caseCount = 500; // 500 cases over 1 year
            
            for (int i = 1; i <= caseCount; i++)
            {
                var createdAt = DateTime.UtcNow.AddDays(-rand.Next(0, 365)).ToUniversalTime();
                var client = clients[rand.Next(clients.Count)];
                var crime = pick(pakistaniCrimes);
                var station = pick(pakistaniPoliceStations);
                var court = pick(pakistaniCourts);
                var city = pick(pakistaniCities);
                var status = i <= 150 ? CaseStatus.Closed
                    : i <= 280 ? CaseStatus.VerdictIssued
                    : i <= 360 ? CaseStatus.Hearing
                    : i <= 420 ? CaseStatus.Investigation
                    : earlyStatuses[rand.Next(earlyStatuses.Length)];

                var title = crime + " in " + city;
                var description = $"FIR registered at {station} under {crime}. The investigation team is collecting witness statements, CCTV footage, and forensic evidence. " +
                                  $"The matter is currently routed through {court} and is being reviewed under Pakistan Penal Code procedures. " +
                                  "Additional case notes recorded.";

                var updatedAt = createdAt.AddDays(rand.Next(0, Math.Max(1, (int)(DateTime.UtcNow - createdAt).TotalDays + 1))).ToUniversalTime();

                var c = new Case
                {
                    Country = "Pakistan",
                    CaseNumber = $"LF-{createdAt.Year}-{i:D4}",
                    Title = title,
                    Description = description,
                    Status = status,
                    ClientId = client.Id,
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt
                };

                // Determine assigned officials based on status
                if (status >= CaseStatus.AssignedToJudgeAndPolice || status == CaseStatus.Closed)
                {
                    if (judges.Any()) c.JudgeId = judges[rand.Next(judges.Count)].Id;
                    if (police.Any()) c.PoliceId = police[rand.Next(police.Count)].Id;
                    if (clerks.Any()) c.ClerkId = clerks[rand.Next(clerks.Count)].Id;
                }

                if (status >= CaseStatus.LawyerAccepted || status == CaseStatus.AssignedToLawyer || status == CaseStatus.Closed)
                {
                    if (lawyers.Any()) c.LawyerId = lawyers[rand.Next(lawyers.Count)].Id;
                }

                casesToInsert.Add(c);
            }

            // Save cases first so we get IDs
            foreach (var batch in casesToInsert.Chunk(500))
            {
                _context.Cases.AddRange(batch);
                await _context.SaveChangesAsync();
            }

            // Now that cases have IDs, we can generate related data
            foreach (var c in casesToInsert)
            {
                // Documents
                int docCount = rand.Next(1, 6);
                var docTypes = new[] { "FIR", "Evidence", "Report", "Statement" };
                for (int d = 0; d < docCount; d++)
                {
                    var file = $"doc-{rand.Next(1000, 9999)}.pdf";
                    var uploadedAt = c.CreatedAt.AddDays(rand.Next(0, Math.Max(1, (int)(DateTime.UtcNow - c.CreatedAt).TotalDays + 1))).ToUniversalTime();
                    docsToInsert.Add(new Document
                    {
                        CaseId = c.Id,
                        FileName = file,
                        FilePath = $"/uploads/{file}",
                        DocumentType = docTypes[rand.Next(docTypes.Length)],
                        UploadedById = c.ClientId,
                        UploadedAt = uploadedAt,
                        IsApproved = true
                    });
                }

                // Hearings
                if (c.Status >= CaseStatus.Hearing || c.Status == CaseStatus.VerdictIssued || c.Status == CaseStatus.Closed)
                {
                    int hearingCount = rand.Next(1, 4);
                    var hearingStatuses = new[] { "Scheduled", "Completed", "Adjourned" };
                    for (int h = 0; h < hearingCount; h++)
                    {
                        var hearingDate = c.CreatedAt.AddDays(rand.Next(0, 31)).ToUniversalTime();
                        hearingsToInsert.Add(new Hearing
                        {
                            CaseId = c.Id,
                            HearingDate = hearingDate,
                            Location = $"{pick(pakistaniCities)} Courtroom {rand.Next(1, 11)}",
                            Notes = "Hearing notes recorded.",
                            Status = hearingStatuses[rand.Next(hearingStatuses.Length)],
                            CreatedAt = c.CreatedAt
                        });
                    }
                }

                // Police Reports
                if (c.Status >= CaseStatus.Investigation && c.PoliceId != null)
                {
                        var reportStation = pick(pakistaniPoliceStations);
                        var reportCrime = pick(pakistaniCrimes);
                        var firNumber = $"FIR-{rand.Next(1000, 9999)}-{c.CreatedAt.Year}";
                        var reportCreatedAt = c.CreatedAt.AddDays(rand.Next(0, Math.Max(1, (int)(DateTime.UtcNow - c.CreatedAt).TotalDays + 1))).ToUniversalTime();
                        reportsToInsert.Add(new PoliceReport
                        {
                            CaseId = c.Id,
                            OfficerId = c.PoliceId,
                            Summary = $"Investigation report filed at {reportStation} (FIR No. {firNumber}). {reportCrime}. The team documented witness testimony, mobile records, and scene photographs. " +
                                      (c.Status >= CaseStatus.VerdictIssued ? "The criminal record has been updated and forwarded to the judicial review panel." : "The inquiry is ongoing and all findings are being compiled for review."),
                            CriminalRecordUpdated = c.Status >= CaseStatus.VerdictIssued,
                            CreatedAt = reportCreatedAt
                        });
                }

                // Verdicts
                if ((c.Status == CaseStatus.VerdictIssued || c.Status == CaseStatus.Closed) && c.JudgeId != null)
                {
                    var verdictTypes = new[] { VerdictType.Guilty, VerdictType.Acquitted, VerdictType.Dismissed, VerdictType.Appealed };
                    var verdictType = verdictTypes[rand.Next(verdictTypes.Length)];
                    var sentence = pakistaniSentences[rand.Next(pakistaniSentences.Length)];
                    var details = verdictType switch
                    {
                        VerdictType.Guilty => $"After hearing the evidence, the court found the accused guilty under the relevant sections of the Pakistan Penal Code and imposed {sentence}.",
                        VerdictType.Acquitted => $"The court acquitted the accused due to insufficient evidence and contradictions in witness statements, in accordance with Pakistan Penal Code standards.",
                        VerdictType.Dismissed => $"The case was dismissed for lack of prosecutable evidence and procedural concerns under the PPC.",
                        VerdictType.Appealed => $"The verdict is under appeal pending review by the higher judiciary, with questions on evidentiary sufficiency and sentencing.",
                        _ => "Verdict details recorded."
                    };

                    verdictsToInsert.Add(new Verdict
                    {
                        CaseId = c.Id,
                        JudgeId = c.JudgeId,
                        Type = verdictType,
                        Details = details,
                        IsPublished = c.Status == CaseStatus.Closed,
                        IssuedAt = (c.UpdatedAt ?? DateTime.UtcNow).AddDays(-1),
                        PublishedAt = c.Status == CaseStatus.Closed ? (DateTime?)(c.UpdatedAt ?? DateTime.UtcNow) : null
                    });
                }
            }

            // Add related data in batches to avoid overwhelming the context
            foreach (var batch in docsToInsert.Chunk(500)) { _context.Documents.AddRange(batch); await _context.SaveChangesAsync(); }
            foreach (var batch in hearingsToInsert.Chunk(500)) { _context.Hearings.AddRange(batch); await _context.SaveChangesAsync(); }
            foreach (var batch in reportsToInsert.Chunk(500)) { _context.PoliceReports.AddRange(batch); await _context.SaveChangesAsync(); }
            foreach (var batch in verdictsToInsert.Chunk(500)) { _context.Verdicts.AddRange(batch); await _context.SaveChangesAsync(); }
        }
    }
}
