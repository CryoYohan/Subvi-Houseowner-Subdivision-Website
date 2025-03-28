using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ELNET1_GROUP_PROJECT.Models;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("BILL")]
    public class Bill
    {
        [Key]
        [Column("BILL_ID")]
        public int BillId { get; set; }

        [Column("BILL_NAME")]
        [Required]
        public string BillName { get; set; }

        [Column("AMOUNT")]
        public decimal BillAmount { get; set; }

        [Column("DUE_DATE")]
        [Required]
        public string DueDate { get; set; }

        [Column("STATUS")]
        [Required]
        public string Status { get; set; }

        [Column("USER_ID")]
        [Required]
        public int UserId { get; set; }
    }
}
