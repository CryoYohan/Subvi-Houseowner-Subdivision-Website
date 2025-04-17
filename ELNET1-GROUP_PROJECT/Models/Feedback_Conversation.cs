using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ELNET1_GROUP_PROJECT.Models;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("FEEDBACK_CONVERSATION")]
    public class Feedback_Conversation
    {
        [Key]
        [Column("CONVO_ID")]
        public int ConvoId { get; set; }

        [ForeignKey("Feedback")]
        [Column("FEEDBACK_ID")]
        public int FeedbackId { get; set; }

        [Column("SENDER_ROLE")]
        [Required]
        [StringLength(10)]
        public string SenderRole { get; set; }

        [ForeignKey("User")]
        [Column("USER_ID")]
        public int? UserId { get; set; }

        [Column("MESSAGE")]
        [Required]
        public string Message { get; set; }

        [Column("DATE_SENT")]
        public DateTime DateSent { get; set; }
    }
}