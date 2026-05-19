using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawFlow.Models
{
    public class Verdict : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [ForeignKey("CaseId")]
        public virtual Case? Case { get; set; }

        [Required]
        public string JudgeId { get; set; } = string.Empty;

        [ForeignKey("JudgeId")]
        public virtual ApplicationUser? Judge { get; set; }

        [Required]
        public VerdictType Type { get; set; }

        [Required]
        public string Details { get; set; } = string.Empty;

        public bool IsPublished { get; set; } = false;

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PublishedAt { get; set; }
    }
}
