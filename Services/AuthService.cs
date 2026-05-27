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
            try
            {
                _context.Users.RemoveRange(_context.Users);
                _context.Cases.RemoveRange(_context.Cases);
                _context.Documents.RemoveRange(_context.Documents);
                _context.Notifications.RemoveRange(_context.Notifications);
                _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
                _context.PoliceReports.RemoveRange(_context.PoliceReports);
                _context.Verdicts.RemoveRange(_context.Verdicts);
                _context.Hearings.RemoveRange(_context.Hearings);
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                // Ignored: tables may not exist yet.
            }

            // 1. Seed base static users so login always works for these roles
            var staticUsers = new List<(string Username, string FullName, UserRole Role, string Spec, string Badge, string Dept)>
            {
                ("admin", "Ahmed Khan", UserRole.Admin, "", "", ""),
                ("judge_ali", "Ali Raza", UserRole.Judge, "Criminal Law", "", ""),
                ("judge_saba", "Saba Malik", UserRole.Judge, "Civil Law", "", ""),
                ("lawyer_faisal", "Faisal Ahmed", UserRole.Lawyer, "Defense Attorney", "", ""),
                ("lawyer_nadia", "Nadia Hussain", UserRole.Lawyer, "Corporate Law", "", ""),
                ("lawyer_uzair", "Uzair Siddiqui", UserRole.Lawyer, "Family Law", "", ""),
                ("client_zaheer", "Zaheer Ahmed", UserRole.Client, "", "", ""),
                ("client_fariha", "Fariha Begum", UserRole.Client, "", "", ""),
                ("client_haneef", "Haneef Iqbal", UserRole.Client, "", "", ""),
                ("client_samira", "Samira Khan", UserRole.Client, "", "", ""),
                ("police_rahim", "Rahim Ali", UserRole.Police, "", "PKP-001", ""),
                ("police_lara", "Lara Siddiqi", UserRole.Police, "", "PKP-002", ""),
                ("police_hammad", "Hammad Saleem", UserRole.Police, "", "PKP-003", ""),
                ("police_farooq", "Farooq Zaman", UserRole.Police, "", "PKP-004", ""),
                ("clerk_ahmad", "Ahmad Shah", UserRole.Clerk, "", "", "Records Department"),
                ("clerk_laila", "Laila Qureshi", UserRole.Clerk, "", "", "Scheduling Department"),
                ("clerk_osama", "Osama Tariq", UserRole.Clerk, "", "", "Evidence Management"),
                ("clerk_fahad", "Fahad Iqbal", UserRole.Clerk, "", "", "Administrative Support")
            };

            foreach (var u in staticUsers)
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

            // 2. Generate extra users manually (avoid Bogus locales)
            var pakistaniFirstNames = new[] { "Adeel", "Aisha", "Ali", "Amina", "Bilal", "Fatima", "Hassan", "Imran", "Khadija", "Laila", "Mahnoor", "Mariam", "Nadia", "Osama", "Rahim", "Rania", "Saba", "Saima", "Uzair", "Zainab" };
            var pakistaniLastNames = new[] { "Ahmed", "Ali", "Hussain", "Khan", "Malik", "Qureshi", "Raza", "Siddiqui", "Tariq", "Yousaf", "Zaman", "Begum", "Shaikh", "Saeed", "Nawaz", "Javed", "Khurshid" };
            var rand = new Random();
            var extraUsers = new List<ApplicationUser>();

            string RandomName()
            {
                return pakistaniFirstNames[rand.Next(pakistaniFirstNames.Length)] + " " + pakistaniLastNames[rand.Next(pakistaniLastNames.Length)];
            }

            string RandomUsername(string suffix = "")
            {
                var first = pakistaniFirstNames[rand.Next(pakistaniFirstNames.Length)].ToLower();
                var last = pakistaniLastNames[rand.Next(pakistaniLastNames.Length)].ToLower();
                return $"{first}.{last}{rand.Next(100, 999)}{suffix}";
            }

            DateTime RandomPastDate(int yearsBack)
            {
                return DateTime.UtcNow.AddDays(-rand.Next(0, 365 * yearsBack)).ToUniversalTime();
            }

            // Generate 150 clients
            for (int i = 0; i < 150; i++)
            {
                var fullName = RandomName();
                var userName = RandomUsername();
                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = $"{userName}@lawflow.pk",
                    FullName = fullName,
                    Role = UserRole.Client,
                    Country = "Pakistan",
                    IsActive = true,
                    CreatedAt = RandomPastDate(5)
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 30 lawyers
            var lawyerSpecs = new[] { "Corporate Law", "Criminal Law", "Family Law", "Real Estate Law", "Intellectual Property" };
            for (int i = 0; i < 30; i++)
            {
                var fullName = RandomName();
                var userName = RandomUsername("_lawyer");
                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = $"{userName}@lawflow.pk",
                    FullName = fullName,
                    Role = UserRole.Lawyer,
                    Country = "Pakistan",
                    Specialization = lawyerSpecs[rand.Next(lawyerSpecs.Length)],
                    IsActive = true,
                    CreatedAt = RandomPastDate(5)
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 15 judges
            for (int i = 0; i < 15; i++)
            {
                var fullName = RandomName();
                var userName = RandomUsername("_judge");
                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = $"{userName}@lawflow.pk",
                    FullName = "Hon. " + fullName,
                    Role = UserRole.Judge,
                    Country = "Pakistan",
                    IsActive = true,
                    CreatedAt = RandomPastDate(5)
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 30 police
            for (int i = 0; i < 30; i++)
            {
                var fullName = RandomName();
                var userName = RandomUsername("_police");
                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = $"{userName}@lawflow.pk",
                    FullName = "Officer " + fullName,
                    Role = UserRole.Police,
                    Country = "Pakistan",
                    BadgeNumber = "PKP-" + rand.Next(100, 9999),
                    IsActive = true,
                    CreatedAt = RandomPastDate(5)
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 15 clerks
            var clerkDepts = new[] { "Records Department", "Scheduling Department", "Evidence Management", "Public Relations" };
            for (int i = 0; i < 15; i++)
            {
                var fullName = RandomName();
                var userName = RandomUsername("_clerk");
                var user = new ApplicationUser
                {
                    UserName = userName,
                    Email = $"{userName}@lawflow.pk",
                    FullName = fullName,
                    Role = UserRole.Clerk,
                    Country = "Pakistan",
                    Department = clerkDepts[rand.Next(clerkDepts.Length)],
                    IsActive = true,
                    CreatedAt = RandomPastDate(5)
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            _context.Users.AddRange(extraUsers);
            await _context.SaveChangesAsync();

            return true;
        }


    }
}
