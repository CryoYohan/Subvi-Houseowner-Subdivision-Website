using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("APPLICATIONS")]
    public class Applications
    {
        [Key]
        [Column("APPLICATION_ID")]
        public int ApplicationId { get; set; }
        [Column("PERSON_ID")]
        public int PersonId { get; set; }
        [Column("LOT_ID")]
        public int LotId { get; set; }
        [Column("DATE_APPLIED")]
        public DateTime DateApplied { get; set; }
        [Column("REMARKS")]
        public string? Remarks { get; set; }
    }
}
