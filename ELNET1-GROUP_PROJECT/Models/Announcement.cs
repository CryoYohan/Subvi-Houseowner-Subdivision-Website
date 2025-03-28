using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("ANNOUNCEMENT")]
    public class Announcement
    {
        [Key]
        [Column("ANNOUNCEMENT_ID")]
        public int AnnouncementId { get; set; } 

        [Column("TITLE")]
        public string Title { get; set; }

        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Column("DATE_POSTED")]
        public string DatePosted { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }
    }
}
