using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("LOT")]
    public class Lot
    {
        [Key]
        [Column("LOT_ID")]
        public int LotId { get; set; }
        [Column("BLOCK_NUMBER")]
        public string BlockNumber { get; set; }
        [Column("LOT_NUMBER")]
        public string LotNumber { get; set; }
        [Column("SIZE_SQM")]
        public decimal SizeSqm { get; set; }
        [Column("PRICE")]
        public decimal Price { get; set; }
        [Column("STATUS")]
        public string? Status { get; set; } = "Available";
        [Column("DESCRIPTION")]
        public string Description { get; set; }

        [Column("CREATED_AT")]
        public DateTime? CreatedAt { get; set; }
    }
}
