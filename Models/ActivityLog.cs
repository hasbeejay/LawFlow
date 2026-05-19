using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawFlow.Models
{
    public class ActivityLog : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        public string Action { get; set; } = string.Empty; // e.g. "Lawyer Accepted Case", "Verdict Issued"

        public string Details { get; set; } = string.Empty;

    }
}
