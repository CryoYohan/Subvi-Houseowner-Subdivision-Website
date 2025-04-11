using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("VOTES")]
    public class Vote
    {
        [Key]
        [Column("VOTE_ID")]
        public int VoteId { get; set; }

        [Column("VOTE_DATE")]
        [Required]
        public DateTime VoteDate { get; set; }

        [Column("POLL_ID")]
        [Required]
        public int PollId { get; set; }

        [Column("USER_ID")]
        [Required]
        public int UserId { get; set; }

        [Column("CHOICE_ID")]
        [Required]
        public int ChoiceId { get; set; }
    }
}
