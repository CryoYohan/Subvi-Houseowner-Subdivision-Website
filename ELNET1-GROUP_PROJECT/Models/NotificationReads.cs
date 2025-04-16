using System;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using ELNET1_GROUP_PROJECT.Controllers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("NOTIFICATION_READS")]
    public class NotificationReads
    {
        [Key]
        [Column("READ_ID")]
        public int ReadId { get; set; }
        
        [Column("NOTIFICATION_ID")]
        public int NotificationId { get; set; }
        [Column("USER_ID")]
        public int UserId { get; set; }
        
        [Column("DATE_READ")]
        public DateTime DateRead { get; set; }
    }
}
