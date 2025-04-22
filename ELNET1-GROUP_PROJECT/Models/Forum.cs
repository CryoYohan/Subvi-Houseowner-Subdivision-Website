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

        // Foreign key to User_Account
        [ForeignKey("UserId")]
        public User_Account UserAccount { get; set; }

        // Convenience props using nested User_Info
        [NotMapped]
        public string Profile => UserAccount?.User_Info?.Profile;

        [NotMapped]
        public string Firstname => UserAccount?.User_Info?.Firstname;

        [NotMapped]
        public string Lastname => UserAccount?.User_Info?.Lastname;

        [NotMapped]
        public string FullName => $"{UserAccount?.User_Info?.Firstname} {UserAccount?.User_Info?.Lastname}";
    }

}
