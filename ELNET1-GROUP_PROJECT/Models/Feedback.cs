using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("FEEDBACK")]
    public class Feedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("FEEDBACK_ID")]
        public int FeedbackId { get; set; }

        [Required]
        [StringLength(15)]
        [Column("FEEDBACK_TYPE")]
        public string FeedbackType { get; set; }

        [Required]
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Required]
        [Column("STATUS")]
        public bool Status { get; set; } = true;

        [Required]
        [Column("DATE_SUBMITTED")]
        public string DateSubmitted { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [Required]
        [ForeignKey("User")]
        [Column("USER_ID")]
        public int UserId { get; set; }

        // Optional: Rating (only for Compliments)
        [Column("RATING")]
        public int? Rating { get; set; }
    }
}
