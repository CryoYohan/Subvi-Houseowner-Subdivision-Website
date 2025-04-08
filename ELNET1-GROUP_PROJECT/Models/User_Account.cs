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
        [Column("FIRSTNAME")]
        [Required]
        public string Firstname { get; set; }
        [Column("LASTNAME")]
        [Required]
        public string Lastname { get;set; }
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
        [Column("ADDRESS")]
        public string Address { get; set; }
        [Column("CONTACT")]
        public string PhoneNumber { get; set; }

    }
}
