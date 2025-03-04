using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ELNET1_GROUP_PROJECT.Data; // Adjust namespace based on your project
using ELNET1_GROUP_PROJECT.Models;
using Subvi.Models;

namespace ELNET1_GROUP_PROJECT.Controllers
{
    [Route("api/calendar")]
    [ApiController]
    public class CalendarController : ControllerBase
    {
        private readonly MyAppDBContext _context;

        public CalendarController(MyAppDBContext context)
        {
            _context = context;
        }

        [HttpGet("schedules")]
        public async Task<IActionResult> GetSchedules()
        {
            try
            {
                var schedules = new Dictionary<string, ScheduleData>();

                // Fetch Events
                var events = await _context.Event_Calendar
                    .Select(e => new { Date = e.DateTime.Date, e.Description })
                    .ToListAsync();

                foreach (var ev in events)
                {
                    string dateKey = ev.Date.ToString("yyyy-MM-dd");
                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Events.Add(ev.Description);
                }

                // Fetch Reservations
                var reservations = await _context.Reservations
                    .Select(r => new { Date = r.DateTime.Date, Time = r.DateTime.ToString("HH:mm:ss") }) // Fetch Time
                    .ToListAsync();

                foreach (var res in reservations)
                {
                    string dateKey = res.Date.ToString("yyyy-MM-dd");
                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Reservations += 1;
                    schedules[dateKey].ReservationDateTime.Add(res.Time); // Store Time
                }

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching schedules.", error = ex.Message });
            }
        }
    }
}
