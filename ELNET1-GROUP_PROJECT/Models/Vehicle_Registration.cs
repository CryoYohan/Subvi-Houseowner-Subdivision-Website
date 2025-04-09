using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("VEHICLE_REGISTRATION")]
    public class VehicleRegistration
    {
        [Key] 
        [Column("VEHICLE_ID")]
        public int VehicleId { get; set; }

        [Required]
        [MaxLength(15)]
        [Column("PLATE_NUMBER")]
        public string PlateNumber { get; set; }

        [Required]
        [MaxLength(20)]
        [Column("TYPE")]
        public string Type { get; set; }

        [MaxLength(20)]
        [Column("STATUS")]
        public string? Status { get; set; } = "Active";

        [Column("USER_ID")]
        public int UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("COLOR")]
        public string Color { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("CAR_BRAND")]
        public string CarBrand { get; set; }

        [ForeignKey("UserId")]
        public User_Account? UserAccount { get; set; }
    }


}
