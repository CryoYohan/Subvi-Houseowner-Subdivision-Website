using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("REPLIES")]
    public class Replies
    {
        [Key]
        [Column("REPLY_ID")]
        public int ReplyId { get; set; }

        [Column("CONTENT")]
        public string Content { get; set; }

        [Column("DATE")]
        public DateTime Date { get; set; }

        [Column("POST_ID")]
        public int PostId { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }
    }
}
