namespace Subvi.Models
{
    public class ScheduleData
    {
        public List<string> Events { get; set; } = new List<string>();
        public int Reservations { get; set; } = 0;
        public List<string> ReservationDateTime { get; set; } = new List<string>(); 
    }


}
