using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("USER_INFO")]
    public class User_Info
    {
        [Key]
        [Column("PERSON_ID")]
        public int PersonId { get; set; }
        [Column("USER_ACCOUNT_ID")]
        public int? UserAccountId { get; set; }
        public User_Account User_Accounts { get; set; }
        [Column("PROFILE")]
        public string? Profile { get; set; }
        [Column("FIRSTNAME")]
        [Required]
        public string Firstname { get; set; }
        [Column("LASTNAME")]
        [Required]
        public string Lastname { get;set; }
        [Required]
        public string Address { get; set; }
        [Column("CONTACT")]
        public string PhoneNumber { get; set; }
        [Column("DATE_TIME")]
        public DateTime DateCreated { get; set; }

    }
}
