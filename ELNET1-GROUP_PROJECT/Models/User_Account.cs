using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("USER_ACCOUNT")]
    public class User_Account
    {
        [Key]
        [Column("USER_ID")]
        public int Id { get; set; }
        [Column("USERNAME")]
        public string? Username { get; set; }
        [Column("EMAIL")]
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Column("PASSWORD")]
        [Required]
        public string Password { get; set; }
        [Column("ROLE")]
        [Required]
        public string Role { get; set; }
        [Column("DATE_TIME")]
        public DateTime DateRegistered { get; set; }
        [Column("STATUS")]
        public string? Status { get; set; } = "ACTIVE";
        public User_Info User_Info { get; set; }
        public ICollection<Forum>? Forum { get; set; }
    }
}
