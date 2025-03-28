using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("FORUM")]
    public class Forum
    {
        [Key]
        [Column("POST_ID")]
        public int PostId { get; set; }

        [Column("TITLE")]
        public string Title { get; set; }

        [Column("CONTENT")]
        public string Content { get; set; }

        [Column("DATE_POSTED")]
        public DateTime DatePosted { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }
    }
}
