namespace Subvi.Models
{
    public class ScheduleData
    {
        public List<EventItem> Events { get; set; } = new List<EventItem>();
        public int Reservations { get; set; } = 0;
        public List<string> ReservationDateTime { get; set; } = new List<string>(); 
    }


}
