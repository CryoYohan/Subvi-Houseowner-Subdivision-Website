using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("LIKE")]
    public class Like
    {
        [Key]
        [Column("LIKE_ID")]
        public int LikeId { get; set; }

        [Column("POST_ID")]
        public int PostId { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }
    }
}
