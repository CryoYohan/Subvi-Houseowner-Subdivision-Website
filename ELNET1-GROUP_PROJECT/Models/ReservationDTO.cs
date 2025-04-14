using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class ReservationDto
    {
        public string FacilityName { get; set; }
        public string SelectedDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }
}
