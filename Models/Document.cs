using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawFlow.Models
{
    public class Document : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [ForeignKey("CaseId")]
        public virtual Case? Case { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Required]
        public string UploadedById { get; set; } = string.Empty;

        [ForeignKey("UploadedById")]
        public virtual ApplicationUser? UploadedBy { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty; // FIR, Evidence, VerdictPDF, LegalBrief

        // UploadedAt is covered by CreatedAt in BaseEntity. We'll rename UploadedAt to CreatedAt or just use CreatedAt.
        // But for minimal changes, let's keep UploadedAt and just not map it to CreatedAt or just remove it if we rely on CreatedAt.
        // Wait, other places might use UploadedAt. Let's keep it as an alias or mapped. 
        // Better yet, since we added BaseEntity, let's keep the existing field to avoid breaking UI code, 
        // or just rely on CreatedAt. I will keep it for now. Actually, if I remove it, I might break UI. Let's just leave UploadedAt and inherit BaseEntity.

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public bool? IsApproved { get; set; } // Null = Pending, True = Approved, False = Rejected (for evidence review by Judge)
    }
}
