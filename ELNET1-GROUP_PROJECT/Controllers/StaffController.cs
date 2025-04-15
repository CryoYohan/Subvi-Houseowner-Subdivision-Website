using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ELNET1_GROUP_PROJECT.Controllers;
using System.Security.Claims;
using OfficeOpenXml;
using iText.Layout;
using iText.Layout.Element;
using iText.Kernel.Colors;
using iText.Kernel.Pdf;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.IO;
using System.Reflection.Metadata;

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

    [Route("")]
    [Route("dashboard")]
    public IActionResult Dashboard()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrWhiteSpace(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }

        var Iduser = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(Iduser, out int userId))
        {
            return RedirectToAction("landing");
        }
        var profilePath = _context.User_Accounts
            .Where(u => u.Id == userId)
            .Select(u => u.Profile)
            .FirstOrDefault();

        ViewBag.ProfilePath = profilePath;

        return View();
    }


    [Route("pass/visitors")]
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
                      VisitorName = char.ToUpper(v.VisitorName[0]) + v.VisitorName.Substring(1),
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
            .Where(u => u.Role == "Homeowner" && u.Status == "ACTIVE") 
            .Select(u => new
            {
                UserId = u.Id,
                FirstName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                LastName = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                Email = u.Email
            })
            .ToList();

        return Json(homeowners);
    }

    [HttpGet("getvisitor/{id}")]
    public IActionResult GetVisitor(int id)
    {
        var visitor = (from v in _context.Visitor_Pass
                       join u in _context.User_Accounts on v.UserId equals u.Id
                       where v.VisitorId == id
                       select new
                       {
                           v.VisitorId,
                           v.UserId,
                           FirstName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                           LastName = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                           Email = u.Email,
                           HomeownerName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                           v.VisitorName,
                           v.Relationship
                       }).FirstOrDefault();

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

    [HttpPost("deletevisitor/{id}")]
    public IActionResult DeleteVisitor(int id)
    {
        var visitor = _context.Visitor_Pass.Find(id);
        if (visitor != null)
        {
            // Mark visitor as deleted
            visitor.Status = "Deleted";
            _context.SaveChanges();
            return Json(new { success = true });
        }
        return Json(new { success = false, message = "Visitor not found." });
    }

    [Route("vehicle/registration")]
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

    [HttpGet("vehicle/registration/data/status/{status}")]
    public IActionResult GetVehiclesByStatus(string status)
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }

        // Fetch vehicles by status from the database
        var vehicleList = _context.Vehicle_Registration
            .Where(v => v.Status.ToLower() == status.ToLower())
            .OrderByDescending(v => v.VehicleId)
            .ToList();

        // Return JSON data to be handled by the JavaScript
        return Json(vehicleList);
    }

    // GET: staff/VehicleRegistration/5
    [HttpGet("VehicleRegistration/{id}")]
    public IActionResult GetById(int id)
    {
        var vehicle = (from v in _context.Vehicle_Registration
                       join u in _context.User_Accounts on v.UserId equals u.Id
                       where v.VehicleId == id
                       select new
                       {
                           v.VehicleId,
                           v.PlateNumber,
                           v.Type,
                           v.Status,
                           v.Color,
                           v.CarBrand,
                           v.UserId,
                           FirstName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                           LastName = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                           Email = u.Email,
                           HomeownerName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                       }).FirstOrDefault();

        if (vehicle == null)
        {
            return NotFound();
        }

        return Ok(vehicle);
    }

    [HttpPost("VehicleRegistration")]
    public IActionResult AddVehicle([FromBody] VehicleRegistration vehicle)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // vehicleId will be auto-generated, so no need to pass it in the request body
        // Add the new vehicle to the database
        _context.Vehicle_Registration.Add(vehicle);
        _context.SaveChanges();

        // Return the added vehicle, including the auto-generated vehicleId
        return Ok(vehicle);
    }

    // PUT: staff/VehicleRegistration/5
    [HttpPut("VehicleRegistration/{id}")]
    public IActionResult UpdateVehicle(int id, [FromBody] VehicleRegistration updated)
    {
        if (id != updated.VehicleId) return BadRequest();

        var vehicle = _context.Vehicle_Registration.FirstOrDefault(v => v.VehicleId == id);
        if (vehicle == null) return NotFound();

        vehicle.PlateNumber = updated.PlateNumber;
        vehicle.Type = updated.Type;
        vehicle.Status = updated.Status;
        vehicle.Color = updated.Color;
        vehicle.CarBrand = updated.CarBrand;
        vehicle.UserId = updated.UserId;

        _context.SaveChanges();
        return Ok(vehicle);
    }

    // DELETE: staff/VehicleRegistration/5
    [HttpDelete("VehicleRegistration/{id}")]
    public IActionResult DeleteVehicle(int id)
    {
        var vehicle = _context.Vehicle_Registration.FirstOrDefault(v => v.VehicleId == id);
        if (vehicle == null) return NotFound();

        _context.Vehicle_Registration.Remove(vehicle);
        _context.SaveChanges();
        return Ok();
    }

    [Route("requests/reservation")]
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
                                SchedDate = r.SchedDate.ToString("MM/dd/yyyy"),
                                StartTime = r.StartTime, 
                                EndTime = r.EndTime,     
                                Status = r.Status
                            })
                            .OrderByDescending(r => r.Id)
                            .ToList();

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

        if (request.Status != "Approved" && request.Status != "Declined")
        {
            return BadRequest(new { message = "Invalid status" });
        }

        reservation.Status = request.Status;
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Reservation {id} has been {request.Status.ToLower()}." });
    }

    [Route("requests/services")]
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
            var completedCount = await _context.Service_Request.CountAsync(r => r.Status == "Completed");
            var cancelledCount = await _context.Service_Request.CountAsync(r => r.Status == "Cancelled");
            var rejectedCount = await _context.Service_Request.CountAsync(r => r.Status == "Rejected");

            return Json(new
            {
                pendingCount,
                scheduledCount,
                ongoingCount,
                completedCount,
                cancelledCount,
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
            else if (request.Status == "Scheduled" && !string.IsNullOrEmpty(request.ScheduleDate))
            {
                // Parse the custom datetime format
                if (DateTime.TryParseExact(request.ScheduleDate, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    serviceRequest.ScheduleDate = parsedDate;
                }
                else
                {
                    return BadRequest(new { message = "Invalid date format. Use 'yyyy-MM-dd HH:mm:ss'." });
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { message = "Request updated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error updating service request.", error = ex.Message });
        }
    }

    [Route("bills_and_payments")]
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

    //-------------- BILLS REQ -----------------//
    [HttpGet("bills/get")]
    public IActionResult ModificationGetBills(string status = "Upcoming")
    {
        var today = DateTime.Today;

        // Make sure DbSets are not null
        var bills = _context.Bill?.ToList();
        var users = _context.User_Accounts?.ToList();

        if (bills == null || users == null)
        {
            return StatusCode(500, "Failed to load bills or user accounts.");
        }

        // Update statuses first
        foreach (var bill in bills)
        {
            if (bill.Status == "Deleted" || bill.Status == "Paid")
                continue;

            if (DateTime.TryParse(bill.DueDate, out DateTime dueDate))
            {
                if (dueDate.Date < today && bill.Status != "Overdue")
                {
                    bill.Status = "Overdue";
                }
                else if (dueDate.Date == today && bill.Status != "Due Now")
                {
                    bill.Status = "Due Now";
                }
                else if (dueDate.Date > today && bill.Status != "Upcoming")
                {
                    bill.Status = "Upcoming";
                }
            }
        }

        _context.SaveChanges();

        // Join manually after fetching
        var result = (from bill in bills
                      join user in users
                          on bill.UserId equals user.Id
                      select new
                      {
                          bill.BillId,
                          bill.BillName,
                          bill.DueDate,
                          bill.Status,
                          BillAmount = bill.BillAmount,
                          FullName = char.ToUpper(user.Firstname[0]) + user.Firstname.Substring(1) + " " +
                                     char.ToUpper(user.Lastname[0]) + user.Lastname.Substring(1)
                      }).ToList();

        // Apply filter
        if (status == "Upcoming")
        {
            result = result.Where(b => b.Status == "Upcoming").ToList();
        }
        else if (status == "Due Now")
        {
            result = result.Where(b => b.Status == "Due Now").ToList();
        }
        else if (status == "Overdue")
        {
            result = result.Where(b => b.Status == "Overdue").ToList();
        }

        return Ok(result.OrderByDescending(b => b.BillId));
    }

    [HttpGet("homeowners")]
    public IActionResult GetActiveHomeowners()
    {
        var homeowners = _context.User_Accounts
            .Where(u => u.Role == "Homeowner" && u.Status == "ACTIVE")
            .Select(u => new {
                userId = u.Id,
                fullName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " +
                                     char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                email = u.Email
            })
            .ToList();

        return Ok(homeowners);
    }

    [HttpPost("bills/add")]
    public IActionResult AddBill([FromBody] Bill model)
    {
        model.Status = GetBillStatus(model.DueDate);
        _context.Bill.Add(model);
        _context.SaveChanges();
        return Ok(new { message = "Bill added successfully!" });
    }

    [HttpGet("bills/getbyid/{id}")]
    public IActionResult GetBillById(int id)
    {
        var bill = _context.Bill.FirstOrDefault(b => b.BillId == id);
        if (bill == null) return NotFound();
        return Ok(bill);
    }

    [HttpPut("bills/update")]
    public IActionResult UpdateBill([FromBody] Bill updated)
    {
        var bill = _context.Bill.FirstOrDefault(b => b.BillId == updated.BillId);
        if (bill == null) return NotFound();

        bill.BillName = updated.BillName;
        bill.DueDate = updated.DueDate;
        bill.BillAmount = updated.BillAmount;
        bill.Status = GetBillStatus(updated.DueDate);
        bill.UserId = updated.UserId;
        _context.SaveChanges();

        return Ok(new { message = "Bill updated successfully!" }); 
    }

    [HttpDelete("bills/delete/{id}")]
    public IActionResult DeleteBill(int id)
    {
        var bill = _context.Bill.FirstOrDefault(b => b.BillId == id);
        if (bill == null) return NotFound();

        bill.Status = "Deleted";
        _context.SaveChanges();
        return Ok();
    }

    // Helper
    private string GetBillStatus(string dueDate)
    {
        var date = DateTime.Parse(dueDate).Date;
        if (date > DateTime.Today) return "Upcoming";
        if (date == DateTime.Today) return "Due Now";
        return "Overdue";
    }

    //-------------- PAYMENTS --------------//

    [HttpGet("bills/data/{status}")]
    public async Task<IActionResult> GetBills(string status = "Paid")
    {
        var billsWithUser = from bill in _context.Bill
                            join user in _context.User_Accounts
                                on bill.UserId equals user.Id
                            where _context.Payment.Any(p => p.BillId == bill.BillId)
                            select new
                            {
                                bill.BillId,
                                bill.BillName,
                                bill.DueDate,
                                bill.Status,
                                bill.BillAmount,
                                FullName = char.ToUpper(user.Firstname[0]) + user.Firstname.Substring(1) + " " + char.ToUpper(user.Lastname[0]) + user.Lastname.Substring(1)
                            };

        if (status == "Paid")
        {
            billsWithUser = billsWithUser.Where(b => b.Status == "Paid");
        }
        else if (status == "Not Paid")
        {
            billsWithUser = billsWithUser.Where(b => b.Status != "Paid");
        }

        var result = await billsWithUser
            .OrderByDescending(b => b.BillId)
            .ToListAsync();

        return Ok(result);
    }

    // GET: api/BillPayment/by-bill/5
    [HttpGet("payments/by-bill/{billId}")]
    public async Task<IActionResult> GetPaymentsByBill(int billId)
    {
        // Fetch the payments for the given BillId
        var payments = await _context.Payment
            .Where(p => p.BillId == billId)
            .OrderByDescending(p => p.PaymentId)
            .Select(p => new Payment
            {
                PaymentId = p.PaymentId,
                AmountPaid = p.AmountPaid,
                PaymentStatus = p.PaymentStatus,
                PaymentMethod = p.PaymentMethod,
                DatePaid = p.DatePaid
            })
            .ToListAsync();

        // Calculate the total amount paid
        var totalAmountPaid = payments.Sum(p => p.AmountPaid);

        // Return the payments along with the total amount
        return Ok(new { Payments = payments, TotalAmountPaid = totalAmountPaid });
    }

    [Route("poll_management")]
    public IActionResult Poll()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("polls")]
    public async Task<IActionResult> GetPolls([FromQuery] string status = "active")
    {
        bool isActive = status.ToLower() != "inactive";
        var now = DateTime.Now;

        var allPolls = await _context.Poll.ToListAsync();

        foreach (var poll in allPolls)
        {
            if (DateTime.TryParse(poll.EndDate, out DateTime endDate))
            {
                if (endDate < now && poll.Status == true) // if active but expired
                {
                    poll.Status = false; // set to inactive
                }
            }
        }

        await _context.SaveChangesAsync();

        var filteredPolls = allPolls
            .Where(p => p.Status == isActive)
            .OrderByDescending(p => DateTime.TryParse(p.StartDate, out var start) ? start : DateTime.MinValue)
            .ToList();

        return Ok(filteredPolls);
    }

    [HttpPost("polls")]
    public async Task<IActionResult> AddPoll([FromBody] PollCreateRequest request)
    {
        var Iduser = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(Iduser, out int userId))
        {
            return Unauthorized();
        }

        var normalizedTitle = request.Title.Trim().ToLower();
        var normalizedDescription = request.Description.Trim().ToLower();
        var newChoices = request.Choices.Select(c => c.Trim().ToLower()).OrderBy(c => c).ToList();

        // Get polls with same title + description
        var similarPolls = await _context.Poll
            .Where(p => p.Title.ToLower() == normalizedTitle && p.Description.ToLower() == normalizedDescription)
            .Select(p => new
            {
                p.PollId,
                Choices = _context.Poll_Choice
                    .Where(pc => pc.PollId == p.PollId)
                    .Select(pc => pc.Choice.Trim().ToLower())
                    .ToList()
            })
            .ToListAsync();

        foreach (var poll in similarPolls)
        {
            var existingChoices = poll.Choices.OrderBy(c => c).ToList();
            if (newChoices.SequenceEqual(existingChoices))
            {
                return Conflict(new { message = "The Poll Already Added. Please check first the data before trying again." });
            }
        }

        // Create new poll
        var newPoll = new Poll
        {
            Title = request.Title,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = true,
            UserId = userId
        };

        _context.Poll.Add(newPoll);
        await _context.SaveChangesAsync();

        foreach (var choice in request.Choices)
        {
            _context.Poll_Choice.Add(new Poll_Choice
            {
                Choice = choice.Trim(),
                PollId = newPoll.PollId
            });
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Poll created successfully." });
    }

    [HttpPut("polls/{pollId}")]
    public async Task<IActionResult> UpdatePoll(int pollId, [FromBody] PollCreateRequest request)
    {
        var poll = await _context.Poll.FindAsync(pollId);
        if (poll == null) return NotFound();

        var normalizedTitle = request.Title.Trim().ToLower();
        var normalizedDescription = request.Description.Trim().ToLower();
        var newChoicesRaw = request.Choices
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .ToList();

        var newChoicesNormalized = newChoicesRaw
            .Select(c => c.ToLower())
            .OrderBy(c => c)
            .ToList();

        // Duplicate detection
        var similarPolls = await _context.Poll
            .Where(p => p.PollId != pollId &&
                        p.Title.ToLower() == normalizedTitle &&
                        p.Description.ToLower() == normalizedDescription)
            .Select(p => new
            {
                p.PollId,
                Choices = _context.Poll_Choice
                    .Where(pc => pc.PollId == p.PollId)
                    .Select(pc => pc.Choice.Trim().ToLower())
                    .ToList()
            })
            .ToListAsync();

        foreach (var otherPoll in similarPolls)
        {
            var existingChoices = otherPoll.Choices.OrderBy(c => c).ToList();
            if (newChoicesNormalized.SequenceEqual(existingChoices))
            {
                return Conflict(new { message = "Another poll with the same data. Please check poll data before trying again." });
            }
        }

        // Update main fields
        poll.Title = request.Title;
        poll.Description = request.Description;
        poll.StartDate = request.StartDate;
        poll.EndDate = request.EndDate;

        // Fetch existing choices
        var existingChoicesDetails = await _context.Poll_Choice
            .Where(c => c.PollId == pollId)
            .ToListAsync();

        // === Update choices: rename-safe ===
        // Map normalized text -> existing entity
        var existingChoiceMap = existingChoicesDetails.ToDictionary(
            c => c.Choice.Trim().ToLower(),
            c => c
        );

        var updatedChoiceIds = new HashSet<int>();

        foreach (var (newRaw, index) in newChoicesRaw.Select((val, i) => (val, i)))
        {
            var normalized = newRaw.ToLower();

            // 1. If exact match exists — keep it
            if (existingChoiceMap.TryGetValue(normalized, out var existing))
            {
                updatedChoiceIds.Add(existing.ChoiceId);
                continue;
            }

            // 2. Else: try match by position — rename existing
            if (index < existingChoicesDetails.Count)
            {
                var toRename = existingChoicesDetails[index];
                toRename.Choice = newRaw;
                updatedChoiceIds.Add(toRename.ChoiceId);
            }
            else
            {
                // 3. Add new
                _context.Poll_Choice.Add(new Poll_Choice
                {
                    PollId = pollId,
                    Choice = newRaw
                });
            }
        }

        // Remove old choices that were not reused/renamed, but if someone already voted it will not delete
        foreach (var existing in existingChoicesDetails)
        {
            if (!updatedChoiceIds.Contains(existing.ChoiceId))
            {
                // Check if votes exist for this choice
                bool hasVotes = await _context.Vote
                    .AnyAsync(v => v.ChoiceId == existing.ChoiceId);

                if (hasVotes)
                {
                    continue;
                }

                _context.Poll_Choice.Remove(existing);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Poll updated successfully." });
    }

    //Check if Choice has Already voters
    [HttpGet("check-votes/{choiceId}")]
    public async Task<IActionResult> CheckIfChoiceHasVotes(int choiceId)
    {
        var hasVotes = await _context.Vote
            .AnyAsync(v => v.ChoiceId == choiceId);

        return Ok(new { hasVotes });
    }

    //Soft Delete 
    [HttpPatch("polls/{pollId}")]
    public async Task<IActionResult> SoftDeletePoll(int pollId)
    {
        var poll = await _context.Poll.FindAsync(pollId);
        if (poll == null) return NotFound();

        poll.Status = false;  // Mark as Inactive
        await _context.SaveChangesAsync();
        return Ok(new { message = "Poll deactivated." });
    }

    [HttpGet("polls/details/{pollId}")]
    public async Task<IActionResult> GetPoll(int pollId)
    {
        var poll = await _context.Poll.FindAsync(pollId);
        if (poll == null) return NotFound();
        return Ok(poll);
    }

    [HttpGet("polls/{pollId}/choices")]
    public async Task<IActionResult> GetChoices(int pollId)
    {
        var choices = await _context.Poll_Choice
            .Where(c => c.PollId == pollId)
            .ToListAsync();

        return Ok(choices);
    }

    [HttpGet("polls/vote-percentage/{choiceId}")]
    public async Task<IActionResult> GetVotePercentage(int choiceId)
    {
        var choice = await _context.Poll_Choice.FindAsync(choiceId);
        if (choice == null) return NotFound();

        int totalVotes = await _context.Vote
            .CountAsync(v => v.PollId == choice.PollId);

        int choiceVotes = await _context.Vote
            .CountAsync(v => v.ChoiceId == choiceId);

        double percentage = totalVotes == 0 ? 0.0 : (double)choiceVotes / totalVotes * 100;
        return Ok(new { choiceId, percentage });
    }

    [Route("reports")]
    public IActionResult Reports()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }

        // Total Counts
        ViewBag.TotalReservations = _context.Reservations.Count();
        ViewBag.TotalPayments = _context.Payment.Count();
        ViewBag.TotalFeedbacks = _context.Feedback.Count();
        ViewBag.TotalServiceRequests = _context.Service_Request.Count();
        ViewBag.TotalVehicles = _context.Vehicle_Registration.Count();
        ViewBag.TotalVisitors = _context.Visitor_Pass.Count();

        // Reservation Trends (Last 4 months)
        var reservationTrends = _context.Reservations
            .AsEnumerable() // Forces client-side processing
            .Select(r => new
            {
                // Convert JSType.Date to DateTime and format as YYYY-MM
                Month = DateTime.Parse(r.SchedDate.ToString()).ToString("yyyy-MM")
            })
            .GroupBy(r => r.Month)
            .OrderBy(g => g.Key)
            .TakeLast(4)
            .Select(g => new { Month = g.Key, Count = g.Count() })
            .ToList();

        ViewBag.ReservationMonths = reservationTrends.Select(r => r.Month).ToList();
        ViewBag.ReservationCounts = reservationTrends.Select(r => r.Count).ToList();

        // Payments per Month
        var paymentTrends = _context.Payment
            .AsEnumerable() // Forces client-side evaluation
            .Select(p => new
            {
                Month = DateTime.ParseExact(p.DatePaid, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("yyyy-MM"),
                AmountPaid = p.AmountPaid
            })
            .GroupBy(p => p.Month)
            .OrderBy(g => g.Key)
            .TakeLast(4)
            .Select(g => new { Month = g.Key, Total = g.Sum(p => p.AmountPaid) })
            .ToList();

        ViewBag.PaymentMonths = paymentTrends.Select(p => p.Month).ToList();
        ViewBag.PaymentTotals = paymentTrends.Select(p => p.Total).ToList();

        // Feedback Ratings Breakdown
        var feedbackRatings = _context.Feedback
            .Where(f => f.FeedbackType == "Complement") // Filter by Type = 'Complement'
            .GroupBy(f => f.Rating) // Group by Rating
            .Select(g => new { Rating = g.Key, Count = g.Count() }) // Select Rating and Count
            .ToList();

        ViewBag.FeedbackRatings = feedbackRatings;

        // Vehicle Types Breakdown
        var vehicleTypes = _context.Vehicle_Registration
            .GroupBy(v => v.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToList();

        ViewBag.VehicleTypes = vehicleTypes;

        return View();
    }

    //For fetching data for report
    [HttpPost("GetReportData")]
    public IActionResult GetReportData(string reportType, string status, string startDate, string endDate, string vehicleType, string color)
    {
        var result = new List<object>();

        switch (reportType)
        {
            case "VEHICLE_REGISTRATION":
                var vehicleQuery = _context.Vehicle_Registration
                    .Join(_context.User_Accounts,
                        vehicle => vehicle.UserId,
                        user => user.Id,
                        (vehicle, user) => new { vehicle, user })
                    .AsQueryable();

                if (!string.IsNullOrEmpty(vehicleType))
                {
                    vehicleQuery = vehicleQuery.Where(v => v.vehicle.Type == vehicleType);
                }

                if (!string.IsNullOrEmpty(color))
                {
                    vehicleQuery = vehicleQuery.Where(v => v.vehicle.Color == color);
                }

                result = vehicleQuery.Select(v => new
                {
                    v.vehicle.VehicleId,
                    v.vehicle.PlateNumber,
                    v.vehicle.Type,
                    v.vehicle.Color,
                    v.vehicle.Status,
                    OwnerName = char.ToUpper(v.user.Firstname[0]) + v.user.Firstname.Substring(1).ToLower() + " " +
                            char.ToUpper(v.user.Lastname[0]) + v.user.Lastname.Substring(1).ToLower()
                }).ToList<object>();
                break;

            case "RESERVATIONS":
                DateTime.TryParse(startDate, out var startR);
                DateTime.TryParse(endDate, out var endR);

                var startDateOnly = DateOnly.FromDateTime(startR);
                var endDateOnly = DateOnly.FromDateTime(endR);

                var reservations = _context.Reservations
                    .Join(_context.Facility,
                        res => res.FacilityId,
                        fac => fac.FacilityId,
                        (res, fac) => new { res, fac })
                    .Join(_context.User_Accounts,
                        combined => combined.res.UserId,
                        user => user.Id,
                        (combined, user) => new { combined.res, combined.fac, user })
                    .Where(x =>
                        x.res.Status == status &&
                        x.res.SchedDate >= startDateOnly &&
                        x.res.SchedDate <= endDateOnly)
                    .Select(x => new
                    {
                        x.res.ReservationId,
                        FacilityName = x.fac.FacilityName,
                        DateReserved = x.res.SchedDate.ToString("MM/dd/yyyy"),
                        x.res.StartTime,
                        x.res.EndTime,
                        x.res.Status,
                        ReservedBy = char.ToUpper(x.user.Firstname[0]) + x.user.Firstname.Substring(1).ToLower() + " " +
                                     char.ToUpper(x.user.Lastname[0]) + x.user.Lastname.Substring(1).ToLower()
                    }).ToList<object>();

                result = reservations;
                break;


            case "SERVICE_REQUEST":
                DateTime.TryParse(startDate, out var startS);
                DateTime.TryParse(endDate, out var endS);
                string startDateStr = startS.ToString("yyyy-MM-dd HH:mm:ss");
                string endDateStr = endS.ToString("yyyy-MM-dd HH:mm:ss");

                var services = _context.Service_Request
                    .Join(_context.User_Accounts,
                        request => request.UserId,
                        user => user.Id,
                        (request, user) => new { request, user })
                    .Where(x =>
                        x.request.Status == status &&
                        string.Compare(x.request.DateSubmitted, startDateStr) >= 0 &&
                        string.Compare(x.request.DateSubmitted, endDateStr) <= 0)
                    .Select(x => new
                    {
                        x.request.ServiceRequestId,
                        x.request.ReqType,
                        x.request.Description,
                        x.request.Status,
                        x.request.DateSubmitted,
                        RequestedBy =
                            char.ToUpper(x.user.Firstname[0]) + x.user.Firstname.Substring(1).ToLower() + " " +
                            char.ToUpper(x.user.Lastname[0]) + x.user.Lastname.Substring(1).ToLower()
                    }).ToList<object>();

                result = services;
                break;

            case "VISITOR_PASSES":
                DateTime.TryParse(startDate, out var startV);
                DateTime.TryParse(endDate, out var endV);

                var passes = (from pass in _context.Visitor_Pass
                              join user in _context.User_Accounts on pass.UserId equals user.Id
                              where pass.Status == status && pass.DateTime >= startV && pass.DateTime <= endV
                              select new
                              {
                                  pass.VisitorId,
                                  pass.VisitorName,
                                  pass.DateTime,
                                  pass.Status,
                                  pass.Relationship,
                                  HomeownerName = Capitalize(user.Firstname) + " " + Capitalize(user.Lastname)
                              }).ToList<object>();

                result = passes;
                break;

        }

        return Json(result);
    }

    private static string Capitalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;
        input = input.ToLower();
        return char.ToUpper(input[0]) + input.Substring(1);
    }


    [Route("profile/settings")]
    public IActionResult Settings()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    [HttpGet("get-user")]
    public async Task<IActionResult> GetUser()
    {
        var Iduser = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(Iduser, out int userId))
        {
            return Unauthorized();
        };
        var user = await _context.User_Accounts
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                Profile = u.Profile ?? "",
                u.Firstname,
                u.Lastname,
                u.Email,
                Contact = u.PhoneNumber,
                u.Address
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    [HttpPost("upload-profile")]
    public async Task<IActionResult> UploadProfileImage(IFormFile file)
    {
        var userIdStr = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var user = await _context.User_Accounts.FindAsync(userId);
        if (user == null) return NotFound();

        var ext = Path.GetExtension(file.FileName);
        var name = $"{char.ToUpper(user.Firstname[0])}{user.Lastname}-{userId}{ext}";
        var savePath = Path.Combine("wwwroot/assets/userprofile", name);
        var relativePath = $"/assets/userprofile/{name}";

        using (var stream = new FileStream(savePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        user.Profile = relativePath;
        await _context.SaveChangesAsync();

        return Ok(new { path = relativePath });
    }

    [HttpPut("update-info")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdStr = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        var user = await _context.User_Accounts.FindAsync(userId);
        if (user == null) return NotFound();

        // Email check
        var existingEmail = await _context.User_Accounts
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != userId);
        if (existingEmail != null)
            return Conflict(new { message = "Email already in use by another user." });

        // Full name check
        var nameExists = await _context.User_Accounts
            .FirstOrDefaultAsync(u => u.Firstname.ToLower() == request.Firstname.ToLower() &&
                                      u.Lastname.ToLower() == request.Lastname.ToLower() &&
                                      u.Id != userId);
        if (nameExists != null)
            return Conflict(new { message = "Another user already has the same full name." });

        // Contact check
        var contactExists = await _context.User_Accounts
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.Contact && u.Id != userId);
        if (contactExists != null)
            return Conflict(new { message = "Contact already in use." });

        // Update fields
        user.Firstname = request.Firstname;
        user.Lastname = request.Lastname;
        user.Email = request.Email;
        user.Address = request.Address;
        user.PhoneNumber = request.Contact;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Profile updated successfully." });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] PasswordChangeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("Password is required");

        var userIdStr = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();
        var user = await _context.User_Accounts.FindAsync(userId);

        if (user == null)
            return NotFound();

        user.Password = BCrypt.Net.BCrypt.HashPassword(request.Password);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Password changed successfully" });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
