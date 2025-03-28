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
                    .Select(e => new { Date = e.DateTime.Date, e.Description })
                    .ToListAsync();

                foreach (var ev in events)
                {
                    string dateKey = ev.Date.ToString("yyyy-MM-dd");
                    if (!schedules.ContainsKey(dateKey))
                        schedules[dateKey] = new ScheduleData();

                    schedules[dateKey].Events.Add(ev.Description);
                }

                var reservations = await _context.Reservations
                    .Where(r => r.UserId == userId && r.Status == "Approved")
                    .Select(r => new { Date = r.DateTime.Date, Time = r.DateTime.ToString("HH:mm:ss") })
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
    }
}
