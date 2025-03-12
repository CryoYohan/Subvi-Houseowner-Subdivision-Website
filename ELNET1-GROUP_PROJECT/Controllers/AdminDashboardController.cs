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

        [HttpGet("dashboard-data")]
        public IActionResult GetDashboardData()
        {
            var facilityCount = _context.Facility.Count(f => f.Status == "PENDING");
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
