using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class Service_Request
    {
        [Key]
        [Column("REQUEST_ID")]
        public int ServiceRequestId { get; set; } // Primary Key

        [Column("REQTYPE")]
        public string ReqType { get; set; }

        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Column("STATUS")]
        public string Status { get; set; }

        [Column("DATE_SUBMITTED")]
        public string DateSubmitted { get; set; }

        [Column("ASSIGNED_STAFF_ID")]
        public string AssignedStaffId { get; set; }

        [Column("USER_ID")]
        public string UserId { get; set; }
    }
}
