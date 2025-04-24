using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class UserDataRequest
    {
        // User_Account fields
        public int Id { get; set; }
        public string Email { get; set; }
        public string? Password { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; } = "ACTIVE";
        public DateTime DateRegistered { get; set; }

        // User_Info fields
        public int? PersonId { get; set; }
        public string? Profile { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime? DateCreated { get; set; }
    }

}
