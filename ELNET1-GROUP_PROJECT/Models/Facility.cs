using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class Facility
    {
        [Key]
        [Column("FACILITY_ID")]
        public int FacilityId { get; set; } // Primary Key

        [Column("SCHEDULE")]
        public string Schedule { get; set; }

        [Column("STATUS")]
        public string Status { get; set; }
    }

}
