using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("SERVICE_REQUEST")]
    public class Service_Request
    {
        [Key]
        [Column("REQUEST_ID")]
        public int ServiceRequestId { get; set; } // Primary Key

        [Column("REQ_TYPE")]
        public string ReqType { get; set; }

        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Column("STATUS")]
        public string Status { get; set; }

        [Column("DATE_SUBMITTED")]
        public string DateSubmitted { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; }

        [Column("REJECTED_REASON")]
        public string? RejectedReason { get; set; }

        [Column("SCHEDULE_DATE")]
        public DateTime? ScheduleDate { get; set; }

    }
}
