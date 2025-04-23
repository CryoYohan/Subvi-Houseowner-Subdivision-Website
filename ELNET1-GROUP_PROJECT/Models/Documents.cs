using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("DOCUMENTS")]
    public class Documents
    {
        [Key]
        [Column("DOCUMENT_ID")]
        public int DocumentId { get; set; }
        [Column("PERSON_ID")]
        public int PersonId { get; set; }
        [Column("LOT_ID")]
        public int LotId { get; set; }
        [Column("APPLICATION_ID")]
        public int ApplicationId { get; set; }
        [Column("FILE_NAME")]
        public string FileName { get; set; }
        [Column("FILE_PATH")]
        public string FilePath { get; set; }
        [Column("UPLOAD_DATE")]
        public DateTime UploadDate { get; set; }
    }
}
