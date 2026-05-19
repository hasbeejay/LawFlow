using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawFlow.Models
{
    public class PoliceReport : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [ForeignKey("CaseId")]
        public virtual Case? Case { get; set; }

        [Required]
        public string OfficerId { get; set; } = string.Empty;

        [ForeignKey("OfficerId")]
        public virtual ApplicationUser? Officer { get; set; }

        [Required]
        public string Summary { get; set; } = string.Empty;

        public bool CriminalRecordUpdated { get; set; } = false;

    }
}
