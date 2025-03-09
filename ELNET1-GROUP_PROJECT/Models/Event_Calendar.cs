using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    [Table("EVENT_CALENDAR")]
    public class Event_Calendar
    {
        [Key]
        [Column("EVENT_ID")]
        public int EventId { get; set; }

        [Column("DESCRIPTION")]
        [MaxLength(255)]
        public string Description { get; set; }

        [Column("DATE_TIME")]
        public DateTime DateTime { get; set; }
    }
}
