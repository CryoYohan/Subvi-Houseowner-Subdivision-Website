using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ELNET1_GROUP_PROJECT.Controllers;

[Route("staff")]
public class StaffController : Controller
{
    private readonly MyAppDBContext _context;
    private readonly ILogger<HomeController> _logger;

    public StaffController(MyAppDBContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
        ViewData["Layout"] = "_StaffLayout";
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

    private string GetUserRoleFromToken()
    {
        var token = HttpContext.Request.Cookies["jwt"];
        if (string.IsNullOrEmpty(token)) return null;

        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

        return jwtToken?.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;
    }

    public IActionResult Landing()
    {
        var role = HttpContext.Request.Cookies["UserRole"];
        if (role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("pass/visitors")]
    public IActionResult VisitorsPass()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [HttpGet("getvisitors")]
    public IActionResult GetVisitors(string status)
    {
        var visitors = _context.Visitor_Pass
            .Where(v => status == null || v.Status == status)
            .OrderByDescending(v => v.DateTime)
            .Join(_context.User_Accounts,
                  v => v.UserId,
                  u => u.Id,
                  (v, u) => new
                  {
                      v.VisitorId,
                      v.VisitorName,
                      v.DateTime,
                      v.Relationship,
                      v.Status,
                      FullName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1)
                  })
            .ToList();

        if (!visitors.Any())
        {
            return Json(new { message = "No visitors found." });
        }

        return Json(visitors);
    }

    [HttpGet("gethomeowners")]
    public IActionResult GetHomeowners()
    {
        var homeowners = _context.User_Accounts
            .Where(u => u.Role == "Homeowner" && u.Status == "Active") 
            .Select(u => new
            {
                UserId = u.Id,
                FirstName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                LastName = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1)
            })
            .ToList();

        return Json(homeowners);
    }

    [HttpGet("getvisitor/{id}")]
    public IActionResult GetVisitor(int id)
    {
        var visitor = _context.Visitor_Pass
            .Where(v => v.VisitorId == id)
            .Select(v => new
            {
                VisitorId = v.VisitorId,
                UserId = v.UserId,
                HomeownerName = _context.User_Accounts
                    .Where(u => u.Id == v.UserId)
                    .Select(u => u.Firstname + " " + u.Lastname)
                    .FirstOrDefault(),
                VisitorName = v.VisitorName,
                Relationship = v.Relationship
            })
            .FirstOrDefault();

        if (visitor == null)
        {
            return NotFound();
        }

        return Json(visitor);
    }
    [HttpPost("addvisitor")]
    public IActionResult AddVisitor(int? visitorId, int userId, string visitorName, string relationship)
    {
        _logger.LogInformation("AddVisitor called with VisitorId: {VisitorId}, UserId: {UserId}, VisitorName: '{VisitorName}', Relationship: '{Relationship}'",
        visitorId, userId, visitorName, relationship);
        if (string.IsNullOrWhiteSpace(visitorName))
        {
            return Json(new { success = false, message = "Visitor name is required!" });
        }

        var trimmedVisitorName = visitorName.Trim();

        // Check for existing visitor name
        bool visitorExists = _context.Visitor_Pass
            .Any(v => v.VisitorName == trimmedVisitorName);

        if (visitorExists)
        {
            return Json(new { success = false, message = "Visitor name already exists!" });
        }

        var newVisitor = new Visitor_Pass
        {
            UserId = userId,
            VisitorName = trimmedVisitorName,
            DateTime = DateTime.Now,
            Status = "Active",
            Relationship = relationship
        };

        _context.Visitor_Pass.Add(newVisitor);
        _context.SaveChanges();

        return Json(new { success = true });
    }

    [HttpPost("editvisitor")]
    public IActionResult EditVisitor(int visitorId, int userId, string visitorName, string relationship)
    {
        var visitor = _context.Visitor_Pass.Find(visitorId);
        if (visitor == null)
        {
            return Json(new { success = false, message = "Visitor not found!" });
        }

        if (string.IsNullOrWhiteSpace(visitorName))
        {
            return Json(new { success = false, message = "Visitor name is required!" });
        }

        // Check if another visitor already has the same name
        var trimmedVisitorName = visitorName.Trim();
        bool visitorExists = _context.Visitor_Pass
            .Any(v => v.VisitorId != visitorId && v.VisitorName == trimmedVisitorName);

        if (visitorExists)
        {
            return Json(new { success = false, message = "Visitor name already exists!" });
        }

        visitor.UserId = userId;
        visitor.VisitorName = trimmedVisitorName;
        visitor.Relationship = relationship;
        _context.SaveChanges();

        return Json(new { success = true });
    }

    [HttpPost("deletevisitor")]
    public IActionResult DeleteVisitor(int id)
    {
        var visitor = _context.Visitor_Pass.Find(id);
        if (visitor != null)
        {
            visitor.Status = "Deleted";
            _context.SaveChanges();
        }
        return Json(new { success = true });
    }

    [HttpGet("vehicle/registration")]
    public IActionResult VehicleRegistration()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [HttpGet("requests/reservation")]
    public IActionResult ReservationRequests()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("Reservations")]
    public IActionResult Reservations(string status = "Pending")
    {
        var reservations = (from r in _context.Reservations
                            join f in _context.Facility on r.FacilityId equals f.FacilityId
                            join u in _context.User_Accounts on r.UserId equals u.Id
                            where r.Status == status
                            select new ReservationViewModel
                            {
                                Id = r.ReservationId,
                                FacilityName = char.ToUpper(f.FacilityName[0]) + f.FacilityName.Substring(1),
                                RequestedBy = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                                DateTime = r.DateTime,
                                Status = r.Status
                            }).ToList();

        return Json(reservations);
    }

    [HttpPut("reservations/{id}")]
    public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] ReservationUpdateStatusDTO request)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null)
        {
            return NotFound(new { message = "Reservation not found" });
        }

        if (request.Status != "Scheduled" && request.Status != "Declined")
        {
            return BadRequest(new { message = "Invalid status" });
        }

        reservation.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Reservation {id} has been {request.Status.ToLower()}." });
    }

    [HttpGet("requests/services")]
    public IActionResult ServiceRequests()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    // Get service requests by status
    [HttpGet("getservicerequests")]
    public async Task<IActionResult> GetServiceRequests(string status)
    {
        try
        {
            // Fetch service requests with user info
            var requests = await _context.Service_Request
                .Where(r => r.Status == status)
                .Join(
                    _context.User_Accounts,
                    sr => sr.UserId,
                    ua => ua.Id,
                    (sr, ua) => new
                    {
                        sr.ServiceRequestId,
                        sr.ReqType,
                        sr.Description,
                        sr.Status,
                        sr.DateSubmitted,
                        RejectedReason = sr.RejectedReason ?? string.Empty,
                        ScheduleDate = sr.ScheduleDate != null ? sr.ScheduleDate.Value.ToString("yyyy-MM-dd HH:mm") : null,
                        homeownerName = char.ToUpper(ua.Firstname[0]) + ua.Firstname.Substring(1) + " " +
                                   char.ToUpper(ua.Lastname[0]) + ua.Lastname.Substring(1)
                    }
                )
                .ToListAsync();

            // Count service requests by status
            var pendingCount = await _context.Service_Request.CountAsync(r => r.Status == "Pending");
            var scheduledCount = await _context.Service_Request.CountAsync(r => r.Status == "Scheduled");
            var ongoingCount = await _context.Service_Request.CountAsync(r => r.Status == "Ongoing");
            var finishedCount = await _context.Service_Request.CountAsync(r => r.Status == "Finished");
            var rejectedCount = await _context.Service_Request.CountAsync(r => r.Status == "Rejected");

            return Json(new
            {
                pendingCount,
                scheduledCount,
                ongoingCount,
                finishedCount,
                rejectedCount,
                requests
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error fetching service requests.", error = ex.Message });
        }
    }

    // Update request status (Approve or Reject)
    [HttpPost("updaterequeststatus")]
    public async Task<IActionResult> UpdateRequestStatus([FromBody] UpdateRequestStatusDto request)
    {
        if (request == null)
        {
            return BadRequest(new { message = "Invalid request data." });
        }

        try
        {
            var serviceRequest = await _context.Service_Request
                .FirstOrDefaultAsync(r => r.ServiceRequestId == request.RequestId);

            if (serviceRequest == null)
            {
                return NotFound(new { message = "Service request not found." });
            }

            serviceRequest.Status = request.Status;

            if (request.Status == "Rejected" && !string.IsNullOrEmpty(request.RejectedReason))
            {
                serviceRequest.RejectedReason = request.RejectedReason;
            }
            else if (request.Status == "Scheduled")
            {
                serviceRequest.ScheduleDate = DateTime.Now;  // Set schedule date if needed
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Request updated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating service request.", error = ex.Message });
        }
    }

    [HttpGet("bills_and_payments")]
    public IActionResult BillsAndPayments()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("reports")]
    public IActionResult Reports()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
