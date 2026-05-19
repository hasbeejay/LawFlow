using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LawFlow.Models
{
    public class Message : BaseEntity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CaseId { get; set; }

        [ForeignKey("CaseId")]
        public virtual Case? Case { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [ForeignKey("SenderId")]
        public virtual ApplicationUser? Sender { get; set; }

        [Required]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        public string SenderRole { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Which two-party channel this message belongs to. Determined server-side
        // from the sender's role; clients never get to assert it.
        [Required]
        public ChatChannel Channel { get; set; } = ChatChannel.ClientLawyer;
    }
}
