using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("PAYMENT")]
    public class Payments
    {
        [Column("PAYMENT_ID")]
        public int PaymentId { get; set; }
        [Column("AMOUNT_PAID")]
        public decimal AmountPaid { get; set; }
        [Column("PAYMENT_STATUS")]
        public bool PaymentStatus { get; set; }
        [Column("PAYMENT_METHOD")]
        public string PaymentMethod { get; set; }
        [Column("DATE_PAID")]
        public string DatePaid { get; set; }
        [Column("BILL_ID")]
        public int BillId { get; set; }
        [Column("USER_ID")]
        public int UserId { get; set; }
    }
}
