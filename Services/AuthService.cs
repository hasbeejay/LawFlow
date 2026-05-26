using LawFlow.Authentication;
using LawFlow.Data;
using LawFlow.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Linq;
namespace LawFlow.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher;

        public AuthService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<ApplicationUser>();
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        public async Task<UserSession?> LoginAsync(string username, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

            if (user == null)
                return null;

            var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash ?? "", password);

            if (verificationResult == PasswordVerificationResult.Failed)
                return null;

            return new UserSession
            {
                UserId = user.Id,
                UserName = user.UserName ?? user.Email ?? "",
                FullName = user.FullName,
                Role = user.Role.ToString()
            };
        }

        public async Task<bool> RegisterAsync(string username, string email, string password, string fullName, UserRole role, string? specialization = null, string? badgeNumber = null, string? department = null)
        {
            var exists = await _context.Users.AnyAsync(u => u.UserName == username || u.Email == email);
            if (exists)
                return false;

            var user = new ApplicationUser
            {
                UserName = username,
                Email = email,
                FullName = fullName,
                Role = role,
                Specialization = specialization,
                BadgeNumber = badgeNumber,
                Department = department,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Log action
            var audit = new ActivityLog
            {
                UserId = user.Id,
                Action = "User Registered",
                Details = $"Registered user {username} with role {role}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(audit);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<ApplicationUser>> GetUsersByRoleAsync(UserRole role)
        {
            return await _context.Users.Where(u => u.Role == role && u.IsActive).ToListAsync();
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _context.Users.OrderByDescending(u => u.CreatedAt).ToListAsync();
        }

        public async Task<bool> ToggleUserStatusAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateUserAsync(string userId, string email, string fullName, UserRole role, string? specialization = null, string? badgeNumber = null, string? department = null)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Check if email already used by someone else
            var emailExists = await _context.Users.AnyAsync(u => u.Email == email && u.Id != userId);
            if (emailExists) return false;

            user.Email = email;
            user.FullName = fullName;
            user.Role = role;
            user.Specialization = specialization;
            user.BadgeNumber = badgeNumber;
            user.Department = department;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            // Log action
            var audit = new ActivityLog
            {
                UserId = userId,
                Action = "User Updated by Admin",
                Details = $"Updated user details for username {user.UserName}",
                CreatedAt = DateTime.UtcNow
            };
            _context.ActivityLogs.Add(audit);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> SeedDemoDataAsync()
        {
            // Clear existing data to ensure a clean slate
            _context.Users.RemoveRange(_context.Users);
            // Also clear related tables if needed (Cases, Documents, etc.)
            _context.Cases.RemoveRange(_context.Cases);
            _context.Documents.RemoveRange(_context.Documents);
            _context.Notifications.RemoveRange(_context.Notifications);
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
            _context.PoliceReports.RemoveRange(_context.PoliceReports);
            _context.Verdicts.RemoveRange(_context.Verdicts);
            _context.Hearings.RemoveRange(_context.Hearings);
            await _context.SaveChangesAsync();

            // Seed default accounts for testing the workflow with Pakistani participants
            var usersToSeed = new List<(string Username, string FullName, UserRole Role, string Spec, string Badge, string Dept)>
            {
                ("admin", "Ahmed Khan", UserRole.Admin, "", "", ""),
                // Judges (2)
                ("judge_ali", "Ali Raza", UserRole.Judge, "Criminal Law", "", ""),
                ("judge_saba", "Saba Malik", UserRole.Judge, "Civil Law", "", ""),
                // Lawyers (3)
                ("lawyer_faisal", "Faisal Ahmed", UserRole.Lawyer, "Defense Attorney", "", ""),
                ("lawyer_nadia", "Nadia Hussain", UserRole.Lawyer, "Corporate Law", "", ""),
                ("lawyer_uzair", "Uzair Siddiqui", UserRole.Lawyer, "Family Law", "", ""),
                // Clients (4)
                ("client_zaheer", "Zaheer Ahmed", UserRole.Client, "", "", ""),
                ("client_fariha", "Fariha Begum", UserRole.Client, "", "", ""),
                ("client_haneef", "Haneef Iqbal", UserRole.Client, "", "", ""),
                ("client_samira", "Samira Khan", UserRole.Client, "", "", ""),
                // Police (5)
                ("police_rahim", "Rahim Ali", UserRole.Police, "", "PKP-001", ""),
                ("police_lara", "Lara Siddiqi", UserRole.Police, "", "PKP-002", ""),
                ("police_hammad", "Hammad Saleem", UserRole.Police, "", "PKP-003", ""),
                ("police_farooq", "Farooq Zaman", UserRole.Police, "", "PKP-004", ""),
                ("police_nazir", "Nazir Ahmed", UserRole.Police, "", "PKP-005", ""),
                // Clerks (6)
                ("clerk_ahmad", "Ahmad Shah", UserRole.Clerk, "", "", "Records Department"),
                ("clerk_laila", "Laila Qureshi", UserRole.Clerk, "", "", "Scheduling Department"),
                ("clerk_osama", "Osama Tariq", UserRole.Clerk, "", "", "Evidence Management"),
                ("clerk_nida", "Nida Khan", UserRole.Clerk, "", "", "Document Control"),
                ("clerk_fahad", "Fahad Iqbal", UserRole.Clerk, "", "", "Administrative Support"),
                ("clerk_zara", "Zara Butt", UserRole.Clerk, "", "", "Public Relations")
            };

            foreach (var u in usersToSeed)
            {
                var user = new ApplicationUser
                {
                    UserName = u.Username,
                    Email = $"{u.Username}@lawflow.pk",
                    FullName = u.FullName,
                    Role = u.Role,
                    Specialization = string.IsNullOrEmpty(u.Spec) ? null : u.Spec,
                    BadgeNumber = string.IsNullOrEmpty(u.Badge) ? null : u.Badge,
                    Department = string.IsNullOrEmpty(u.Dept) ? null : u.Dept,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                _context.Users.Add(user);
            }

            await _context.SaveChangesAsync();

            // Seed sample cases spanning 5 years
            var random = new Random();
            var users = _context.Users.ToList();

            // Helper to find user by username
            ApplicationUser FindUser(string username) => users.FirstOrDefault(u => u.UserName == username)!;

            var casesToSeed = new List<(string CaseNumber, string Title, string Description, string ClientUser, string? LawyerUser, string? JudgeUser, DateTime CreatedAt)>
            {
                ("2022-001", "Land Dispute", "Dispute over property boundaries.", "client_zaheer", "lawyer_faisal", "judge_ali", DateTime.UtcNow.AddYears(-4)),
                ("2023-015", "Contract Breach", "Breach of commercial contract.", "client_fariha", "lawyer_nadia", "judge_saba", DateTime.UtcNow.AddYears(-3).AddMonths(-2)),
                ("2024-027", "Family Custody", "Custody battle between parents.", "client_haneef", "lawyer_uzair", "judge_ali", DateTime.UtcNow.AddYears(-2).AddMonths(-5)),
                ("2025-042", "Fraud Investigation", "Investigation of financial fraud.", "client_samira", "lawyer_faisal", "judge_saba", DateTime.UtcNow.AddYears(-1).AddMonths(-1)),
                ("2026-058", "Cybercrime case", "Investigation of hacking incident.", "client_zaheer", "lawyer_nadia", "judge_ali", DateTime.UtcNow.AddMonths(-3))
            };

            foreach (var c in casesToSeed)
            {
                var client = FindUser(c.ClientUser);
                var lawyer = c.LawyerUser != null ? FindUser(c.LawyerUser) : null;
                var judge = c.JudgeUser != null ? FindUser(c.JudgeUser) : null;

                var caseEntity = new Case
                {
                    CaseNumber = c.CaseNumber,
                    Title = c.Title,
                    Description = c.Description,
                    Status = CaseStatus.InProgress,
                    ClientId = client.Id,
                    LawyerId = lawyer?.Id,
                    JudgeId = judge?.Id,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.CreatedAt
                };
                _context.Cases.Add(caseEntity);
                await _context.SaveChangesAsync();

                // Add hearings
                int hearingCount = random.Next(1, 4);
                for (int i = 0; i < hearingCount; i++)
                {
                    var hearing = new Hearing
                    {
                        CaseId = caseEntity.Id,
                        HearingDate = caseEntity.CreatedAt.AddDays(i * 30),
                        Notes = $"Hearing {i + 1} for case {c.CaseNumber}",
                        CreatedAt = caseEntity.CreatedAt.AddDays(i * 30)
                    };
                    _context.Hearings.Add(hearing);
                }

                // Add a document
                var doc = new Document
                {
                    CaseId = caseEntity.Id,
                    FileName = $"{c.Title} Evidence.pdf",
                    FilePath = $"/files/{c.CaseNumber}_evidence.pdf",
                    UploadedAt = caseEntity.CreatedAt.AddDays(1)
                };
                _context.Documents.Add(doc);
            }

            // Final save
            await _context.SaveChangesAsync();

            return true;
        }


    }
}
