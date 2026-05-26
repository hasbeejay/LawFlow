using System;
using System.ComponentModel.DataAnnotations;

namespace LawFlow.Models;

public class Account
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = "User"; // e.g., Admin, Lawyer, Clerk

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
