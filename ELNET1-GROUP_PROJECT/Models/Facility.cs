using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class Facility
    {
        [Key]
        [Column("FACILITY_ID")]
        public int FacilityId { get; set; } // Primary Key

        [Column("IMAGE")]
        public string? Image { get; set; }
        [Column("FACILITY_NAME")]
        public string FacilityName { get; set; }
        [Column("DESCRIPTION")]
        public string Description { get; set; }
        [Column("AVAILABLE_TIME")]
        public string AvailableTime { get; set; }
        [Column("STATUS")]
        public string Status { get; set; }
    }

}
