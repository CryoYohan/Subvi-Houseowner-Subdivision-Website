using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("POLL")]
    public class Poll
    {
        [Key]
        [Column("POLL_ID")]
        public int PollId { get; set; } // Primary Key
        [Column("TITLE")]
        [Required]
        public string Title { get; set; }
        [Column("DESCRIPTION")]
        [Required]
        public string Description { get; set; }
        [Column("START_DATE")]
        [Required]
        public string StartDate { get; set; }
        [Column("END_DATE")]
        [Required]
        public string EndDate { get; set; }
        [Column("STATUS")]
        [Required]
        public bool Status { get; set; }
        [Column("USER_ID")]
        [Required]
        public int UserId { get; set; } 
    }

}
