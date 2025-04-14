using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class ReservationViewModel
    {
        public int Id { get; set; }
        public string FacilityName { get; set; }
        public string RequestedBy { get; set; }
        public string SchedDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Status { get; set; }
    }
}
