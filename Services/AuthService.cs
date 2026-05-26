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
            _context.Cases.RemoveRange(_context.Cases);
            _context.Documents.RemoveRange(_context.Documents);
            _context.Notifications.RemoveRange(_context.Notifications);
            _context.ActivityLogs.RemoveRange(_context.ActivityLogs);
            _context.PoliceReports.RemoveRange(_context.PoliceReports);
            _context.Verdicts.RemoveRange(_context.Verdicts);
            _context.Hearings.RemoveRange(_context.Hearings);
            await _context.SaveChangesAsync();

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

            // 2. Generate extra users with Bogus
            var faker = new Bogus.Faker("en_US"); // Use generic English
            var extraUsers = new List<ApplicationUser>();

            // Generate 150 clients
            for (int i = 0; i < 150; i++)
            {
                var user = new ApplicationUser
                {
                    UserName = faker.Internet.UserName(),
                    Email = faker.Internet.Email(),
                    FullName = faker.Name.FullName(),
                    Role = UserRole.Client,
                    IsActive = true,
                    CreatedAt = faker.Date.Past(5).ToUniversalTime()
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 30 lawyers
            for (int i = 0; i < 30; i++)
            {
                var user = new ApplicationUser
                {
                    UserName = faker.Internet.UserName() + "_lawyer",
                    Email = faker.Internet.Email(),
                    FullName = faker.Name.FullName(),
                    Role = UserRole.Lawyer,
                    Specialization = faker.PickRandom("Corporate Law", "Criminal Law", "Family Law", "Real Estate Law", "Intellectual Property"),
                    IsActive = true,
                    CreatedAt = faker.Date.Past(5).ToUniversalTime()
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 15 judges
            for (int i = 0; i < 15; i++)
            {
                var user = new ApplicationUser
                {
                    UserName = faker.Internet.UserName() + "_judge",
                    Email = faker.Internet.Email(),
                    FullName = "Hon. " + faker.Name.FullName(),
                    Role = UserRole.Judge,
                    IsActive = true,
                    CreatedAt = faker.Date.Past(5).ToUniversalTime()
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 30 police
            for (int i = 0; i < 30; i++)
            {
                var user = new ApplicationUser
                {
                    UserName = faker.Internet.UserName() + "_police",
                    Email = faker.Internet.Email(),
                    FullName = "Officer " + faker.Name.FullName(),
                    Role = UserRole.Police,
                    BadgeNumber = "PKP-" + faker.Random.Number(100, 9999),
                    IsActive = true,
                    CreatedAt = faker.Date.Past(5).ToUniversalTime()
                };
                user.PasswordHash = _passwordHasher.HashPassword(user, "Password123!");
                extraUsers.Add(user);
            }

            // Generate 15 clerks
            for (int i = 0; i < 15; i++)
            {
                var user = new ApplicationUser
                {
                    UserName = faker.Internet.UserName() + "_clerk",
                    Email = faker.Internet.Email(),
                    FullName = faker.Name.FullName(),
                    Role = UserRole.Clerk,
                    Department = faker.PickRandom("Records Department", "Scheduling Department", "Evidence Management", "Public Relations"),
                    IsActive = true,
                    CreatedAt = faker.Date.Past(5).ToUniversalTime()
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
