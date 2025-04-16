using System;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using ELNET1_GROUP_PROJECT.Controllers;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("NOTIFICATIONS")]
    public class Notification
    {
        [Column("NOTIFICATION_ID")]
        public int NotificationId { get; set; }
        [Column("USER_ID")]
        public int? UserId { get; set; }
        [Column("TARGET_ROLE")]
        public string TargetRole { get; set; }
        [Column("TITLE")]
        public string Title { get; set; }
        [Column("MESSAGE")]
        public string Message { get; set; }
        [Column("IS_READ")]
        public bool? IsRead { get; set; } = false;
        [Column("DATE_CREATED")]
        public DateTime DateCreated { get; set; }
        [Column("DATE_READ")]
        public DateTime? DateRead { get; set; }
        [Column("TYPE")]
        public string Type { get; set; }
        [Column("LINK")]
        public string? Link { get; set; }         
    }
}
