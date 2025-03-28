using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ELNET1_GROUP_PROJECT.Models
{
    public class ReservationViewModel
    {
        public int Id { get; set; }
        public string FacilityName { get; set; }
        public string RequestedBy { get; set; }
        public DateTime DateTime { get; set; }
        public string Status { get; set; }
    }
}
