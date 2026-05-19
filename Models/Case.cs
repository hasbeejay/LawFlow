using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawFlow.Models
{
    public class Case : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string CaseNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public CaseStatus Status { get; set; } = CaseStatus.Created;

        // Assigned Roles
        [Required]
        public string ClientId { get; set; } = string.Empty;
        [ForeignKey("ClientId")]
        public virtual ApplicationUser? Client { get; set; }

        public string? LawyerId { get; set; }
        [ForeignKey("LawyerId")]
        public virtual ApplicationUser? Lawyer { get; set; }

        public string? JudgeId { get; set; }
        [ForeignKey("JudgeId")]
        public virtual ApplicationUser? Judge { get; set; }

        public string? PoliceId { get; set; }
        [ForeignKey("PoliceId")]
        public virtual ApplicationUser? Police { get; set; }

        public string? ClerkId { get; set; }
        [ForeignKey("ClerkId")]
        public virtual ApplicationUser? Clerk { get; set; }

        // BaseEntity handles CreatedAt and UpdatedAt

        // Navigation properties
        public virtual ICollection<Hearing> Hearings { get; set; } = new List<Hearing>();
        public virtual ICollection<Document> Documents { get; set; } = new List<Document>();
        public virtual ICollection<PoliceReport> PoliceReports { get; set; } = new List<PoliceReport>();
        public virtual Verdict? Verdict { get; set; }
    }
}
