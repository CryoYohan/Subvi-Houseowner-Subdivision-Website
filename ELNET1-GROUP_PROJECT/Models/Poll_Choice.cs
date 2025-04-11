using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("POLL_CHOICE")]
    public class Poll_Choice
    {
        [Key]
        [Column("CHOICE_ID")]
        public int ChoiceId { get; set; } // Primary Key
        [Column("CHOICE")]
        [Required]
        public string Choice { get; set; }
        [Column("POLL_ID")]
        [Required]
        public int PollId { get; set; } // Foreign Key to Poll
    }

}
