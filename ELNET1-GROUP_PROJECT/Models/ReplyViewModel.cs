using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class ReplyViewModel
    {
        public int ReplyId { get; set; }
        public string Content { get; set; }
        public string Role { get; set; }
        public DateTime Date { get; set; }
        public int UserId { get; set; }
        public string? Profile { get; set; }
        public string FullName { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
}
