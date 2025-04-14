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
        public Date SchedDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; }
    }
}
