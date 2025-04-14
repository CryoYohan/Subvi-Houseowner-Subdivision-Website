using ELNET1_GROUP_PROJECT.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace Subvi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly MyAppDBContext _context;

        public AdminDashboardController(MyAppDBContext context)
        {
            _context = context;
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

        [HttpGet("dashboard-data")]
        public IActionResult GetDashboardData()
        {
            RefreshJwtCookies();
            var facilityCount = _context.Reservations.Count(f => f.Status == "PENDING");
            var totalUsers = _context.User_Accounts.Count();
            var adminCount = _context.User_Accounts.Count(u => u.Role == "Admin");
            var staffCount = _context.User_Accounts.Count(u => u.Role == "Staff");
            var homeownerCount = _context.User_Accounts.Count(u => u.Role == "Homeowner");

            var totalReservations = _context.Reservations.Count();
            var pendingReservations = _context.Reservations.Count(r => r.Status == "PENDING");
            var approvedReservations = _context.Reservations.Count(r => r.Status == "APPROVED");
            var declinedReservations = _context.Reservations.Count(r => r.Status == "DECLINED");

            var totalRequests = _context.Service_Request.Count();
            var pendingRequests = _context.Service_Request.Count(r => r.Status == "PENDING");
            var approvedRequests = _context.Service_Request.Count(r => r.Status == "APPROVED");
            var declinedRequests = _context.Service_Request.Count(r => r.Status == "DECLINED");

            return Ok(new
            {
                facilityCount,
                totalUsers,
                adminCount,
                staffCount,
                homeownerCount,
                totalReservations,
                pendingReservations,
                approvedReservations,
                declinedReservations,
                totalRequests,
                pendingRequests,
                approvedRequests,
                declinedRequests
            });
        }
    }
}
