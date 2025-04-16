using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ELNET1_GROUP_PROJECT.Data; // Adjust namespace based on your project
using ELNET1_GROUP_PROJECT.Models;
using Subvi.Models;
using System.Globalization;

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
                RefreshJwtCookies();

                // Retrieve User ID from Cookies
                var Iduser = HttpContext.Request.Cookies["Id"];
                if (!int.TryParse(Iduser, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var schedules = new Dictionary<string, ScheduleData>();

                // Fetch ALL Events (no UserId filter)
                var events = await _context.Event_Calendar
                    .Select(e => new { EventId = e.EventId, DateTime = e.DateTime, e.Description })
                    .ToListAsync();

                foreach (var ev in events)
                {
                    string dateKey = ev.DateTime.ToString("yyyy-MM-dd");

                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Events.Add(new EventItem
                    {
                        EventId = ev.EventId,
                        Description = ev.Description,
                        DateTime = ev.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                var reservations = await (
                    from r in _context.Reservations
                    join f in _context.Facility on r.FacilityId equals f.FacilityId
                    where r.UserId == userId && r.Status == "Approved"
                    select new
                    {
                        Date = r.SchedDate,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        FacilityName = f.FacilityName 
                    }
                ).ToListAsync();

                foreach (var res in reservations)
                {
                    string dateKey = res.Date.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd");

                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Reservations += 1;

                    schedules[dateKey].ReservationDateTime.Add($"{res.FacilityName}: {res.StartTime} - {res.EndTime}");
                }

                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching schedules.", error = ex.Message });
            }
        }

        [HttpPost]
        [Route("events")]
        public async Task<IActionResult> AddEvent([FromBody] EventDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Description) || string.IsNullOrWhiteSpace(dto.Date_Time))
                return BadRequest("Description and Date_Time are required.");

            var parsedDate = DateTime.Parse(dto.Date_Time);

            var newEvent = new Event_Calendar
            {
                Description = dto.Description,
                DateTime = parsedDate
            };

            _context.Event_Calendar.Add(newEvent);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event added successfully." });
        }

        [HttpPut]
        [Route("events/{id}")]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] EventDto dto)
        {
            var eventToUpdate = await _context.Event_Calendar.FindAsync(id);
            if (eventToUpdate == null)
                return NotFound();

            eventToUpdate.Description = dto.Description;
            eventToUpdate.DateTime = DateTime.Parse(dto.Date_Time);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Event updated successfully." });
        }

        [HttpDelete]
        [Route("events/{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var eventToDelete = await _context.Event_Calendar.FindAsync(id);
            if (eventToDelete == null)
                return NotFound();

            _context.Event_Calendar.Remove(eventToDelete);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Event deleted successfully." });
        }

        public class EventDto
        {
            public string Description { get; set; }
            public string Date_Time { get; set; } // "yyyy-MM-dd HH:mm:ss"
        }

        //Resetting the cookies time
        private void RefreshJwtCookies()
        {
            var token = Request.Cookies["jwt"];
            var role = Request.Cookies["UserRole"];
            var id = Request.Cookies["Id"];

            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(id))
            {
                // Reset cookies with updated expiration
                var expiryMinutes = 15;  // Reset to 15 minutes

                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                };

                Response.Cookies.Append("jwt", token, options);
                Response.Cookies.Append("UserRole", role, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
                Response.Cookies.Append("Id", id, new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(expiryMinutes)
                });
            }
        }

        [HttpGet("admin/schedules")]
        public async Task<IActionResult> GetSchedulesToAdmin()
        {
            try
            {
                RefreshJwtCookies();

                // Retrieve User ID from Cookies
                var Iduser = HttpContext.Request.Cookies["Id"];
                if (!int.TryParse(Iduser, out int userId))
                {
                    return RedirectToAction("Login");
                }

                var schedules = new Dictionary<string, ScheduleData>();

                // Fetch ALL Events (no UserId filter)
                var events = await _context.Event_Calendar
                    .Select(e => new { EventId = e.EventId, DateTime = e.DateTime, e.Description })
                    .ToListAsync();

                foreach (var ev in events)
                {
                    string dateKey = ev.DateTime.ToString("yyyy-MM-dd");

                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Events.Add(new EventItem
                    {
                        EventId = ev.EventId,
                        Description = ev.Description,
                        DateTime = ev.DateTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                var textInfo = CultureInfo.CurrentCulture.TextInfo;

                var reservations = await (
                    from r in _context.Reservations
                    join f in _context.Facility on r.FacilityId equals f.FacilityId
                    join u in _context.User_Accounts on r.UserId equals u.Id
                    where r.Status == "Approved"
                    select new
                    {
                        Date = r.SchedDate,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        FacilityName = f.FacilityName,
                        FirstName = u.Firstname,
                        LastName = u.Lastname
                    }
                ).ToListAsync();

                foreach (var res in reservations)
                {
                    string dateKey = res.Date.ToDateTime(TimeOnly.MinValue).ToString("yyyy-MM-dd");

                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Reservations += 1;

                    // Capitalize full name only
                    var fullName = textInfo.ToTitleCase($"{res.FirstName} {res.LastName}".ToLower());

                    schedules[dateKey].ReservationDateTime.Add($"{res.FacilityName}: {res.StartTime} - {res.EndTime} (by: {fullName})");
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
