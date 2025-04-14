using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("RESERVATIONS")] // Ensure it maps to the correct table
    public class Reservation
    {
        [Key]
        [Column("RESERVATION_ID")]
        public int ReservationId { get; set; }

        [Column("USER_ID")]
        public int UserId { get; set; } 

        [Column("FACILITY_ID")]
        public int FacilityId { get; set; } 

        [Column("SCHED_DATE")]
        public Date SchedDate { get; set; }

        [Column("START_TIME")]
        [MaxLength(10)]
        public string StartTime { get; set; }

        [Column("END_TIME")]
        [MaxLength(10)]
        public string EndTime { get; set; }

        [Column("STATUS")]
        [MaxLength(10)]
        public string Status { get; set; }
    }
}
