using Microsoft.AspNetCore.Identity;
using System;

namespace LawFlow.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; } = string.Empty;
        public string Country { get; set; } = "Pakistan";
        public UserRole Role { get; set; }
        public string? Specialization { get; set; } // For Lawyers/Judges
        public string? BadgeNumber { get; set; }    // For Police
        public string? Department { get; set; }     // For Clerks
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}
