using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("VISITOR_PASSES")]
    public class Visitor_Pass
    {
        [Key]
        [Column("VISITOR_ID")]
        public int VisitorId { get; set; }

        [Column("VISITOR_NAME")]
        public string VisitorName { get; set; }

        [Column("DATE_TIME")]
        public DateTime DateTime { get; set; }

        [Column("STATUS")]
        public string Status { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }

        [Column("RELATIONSHIP")]
        public string Relationship { get; set; }
    }

}
