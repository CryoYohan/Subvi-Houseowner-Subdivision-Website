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

        [Column("HASHTAG")]
        public string? Hashtag { get; set; }

        [Column("CONTENT")]
        public string Content { get; set; }

        [Column("DATE_POSTED")]
        public DateTime DatePosted { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }

        // Foreign key relation to User_Accounts
        [ForeignKey("UserId")]
        public User_Account UserAccount { get; set; }

        // Not mapped convenience props for display
        [NotMapped]
        public string Profile => $"{UserAccount?.Profile}";
        public string Firstname => $"{UserAccount?.Firstname}";
        public string Lastname => $"{UserAccount?.Lastname}";
        public string FullName => $"{UserAccount?.Firstname} {UserAccount?.Lastname}";
    }
}
