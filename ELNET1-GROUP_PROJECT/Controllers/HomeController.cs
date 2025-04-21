using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ELNET1_GROUP_PROJECT.Models;
using ELNET1_GROUP_PROJECT.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http;
using Microsoft.AspNetCore.SignalR;
using ELNET1_GROUP_PROJECT.SignalR;
using Azure.Core;
using Square;
using Square.Apis;
using Square.Exceptions;
using Square.Models;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Net.Mime;
using System.Net.Mail;
using System.Net;
using Paymongo.Sharp.Core.Enums;
using static ELNET1_GROUP_PROJECT.Controllers.HomeController;

namespace ELNET1_GROUP_PROJECT.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly MyAppDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly PayMongoServices _payMongoServices;
        private readonly IHubContext<NotificationHub> _hubContext;

        public HomeController(MyAppDBContext context, ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, PayMongoServices payMongoServices, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            ViewData["Layout"] = "_HomeLayout";
            _payMongoServices = payMongoServices;
            _hubContext = hubContext;
        }

        private string GetUserRoleFromToken()
        {
            var token = HttpContext.Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token)) return null;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;

            return jwtToken?.Claims.FirstOrDefault(c => c.Type == "Role")?.Value;
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

        public IActionResult landing()
        {
            var role = GetUserRoleFromToken();
            if (!string.IsNullOrEmpty(role))
            {
                return RedirectToRoleDashboard(role);
            }
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        public IActionResult Contacts()
        {
            return View();
        }

        public IActionResult dashboard()
        {
            RefreshJwtCookies();
            var role = GetUserRoleFromToken();
            if (role != "Homeowner")
            {
                return RedirectToAction("landing"); // Redirect unauthorized users
            }

            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }
            var profilePath = _context.User_Accounts
                                .FirstOrDefault(u => u.Id == userId)?.Profile;

            ViewBag.ProfilePath = profilePath;

            return View();
        }

        private IActionResult RedirectToRoleDashboard(string role)
        {
            RefreshJwtCookies();
            return role switch
            {
                "Admin" => RedirectToAction("Dashboard", "Admin"),
                "Homeowner" => RedirectToAction("dashboard", "Home"),
                "Staff" => RedirectToAction("Dashboard", "Staff"),
                _ => RedirectToAction("landing", "Home")
            };
        }

        public IActionResult Calendar()
        {
            RefreshJwtCookies();
            return View();
        }
        // There is a seperation controller api for this calendar named CalendarController

        public IActionResult Facilities()
        {
            RefreshJwtCookies();
            var facilities = _context.Facility
                .Where(f => f.Status == "Active")
                .Select(f => new
                {
                    f.FacilityId,
                    Name = f.FacilityName,
                    Description = f.Description,
                    Image = f.Image,
                    Time = f.AvailableTime
                })
                .ToList();

            return View(facilities);
        }

        // Fetch Pending data
        [HttpGet]
        public IActionResult GetPendingFacilities()
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var pendingFacilities = _context.Reservations
                .Where(r => r.Status == "Pending" && r.UserId == userId) // filter by current user
                .Join(
                    _context.Facility,
                    r => r.FacilityId,
                    f => f.FacilityId,
                    (r, f) => new
                    {
                        r.ReservationId,
                        f.FacilityName,
                        DateRequested = r.SchedDate.ToString("MM/dd/yyyy"),
                        r.StartTime,
                        r.EndTime,
                        r.Status
                    }
                )
                .OrderByDescending(r => r.ReservationId)
                .ToList();

            return Json(pendingFacilities);
        }

        //Fetch Approved/Declined Data
        [HttpGet]
        public IActionResult GetFilteredReservations(string status)
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
                return RedirectToAction("landing");

            var query = _context.Reservations
                .Where(r => r.Status == status && r.UserId == userId)
                .Join(_context.Facility,
                    r => r.FacilityId,
                    f => f.FacilityId,
                    (r, f) => new
                    {
                        r.ReservationId,
                        f.FacilityName,
                        DateRequested = r.SchedDate.ToString("MM/dd/yyyy"),
                        r.StartTime,
                        r.EndTime,
                        r.Status
                    })
                .OrderByDescending(r => r.ReservationId)
                .ToList();

            return Json(query);
        }

        // Check for existing facility data
        [HttpGet]
        public IActionResult CheckReservationConflict(string facilityName, string selectedDate, string startTime, string endTime)
        {
            if (!int.TryParse(HttpContext.Request.Cookies["Id"], out int userId))
                return Json(new { success = false, message = "Invalid user session" });

            var facilityId = _context.Facility
                .Where(f => f.FacilityName == facilityName)
                .Select(f => f.FacilityId)
                .FirstOrDefault();

            DateTime date = DateTime.Parse(selectedDate);

            bool conflict = _context.Reservations.Any(r =>
                r.UserId == userId &&
                r.FacilityId == facilityId &&
                r.SchedDate == DateOnly.FromDateTime(date) &&
                r.StartTime == startTime &&
                r.EndTime == endTime
            );

            if (conflict)
            {
                return Json(new { success = false, message = "You already reserved this time slot for the same facility." });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult AddReservation([FromBody] ReservationDto reservation)
        {
            if (!int.TryParse(HttpContext.Request.Cookies["Id"], out int userId))
                return Json(new { success = false, message = "User not authenticated." });

            // Get the user from the database
            var user = _context.User_Accounts.FirstOrDefault(u => u.Id == userId);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            string Capitalize(string input) =>
            string.IsNullOrWhiteSpace(input) ? input : char.ToUpper(input[0]) + input.Substring(1).ToLower();
            string personName = $"{Capitalize(user.Firstname)} {Capitalize(user.Lastname)}";

            // Check if facility already exists
            var existingFacility = _context.Facility
                .FirstOrDefault(f => f.FacilityName == reservation.FacilityName);

            int facilityId;

            if (existingFacility == null)
            {
                // Add new facility
                var newFacility = new Facility
                {
                    FacilityName = reservation.FacilityName
                };

                _context.Facility.Add(newFacility);
                _context.SaveChanges();

                facilityId = newFacility.FacilityId; // Get generated ID
            }
            else
            {
                facilityId = existingFacility.FacilityId;
            }

            // Add reservation
            var newReservation = new Reservation
            {
                SchedDate = DateOnly.FromDateTime(DateTime.Parse(reservation.SelectedDate)),
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                Status = "Pending",
                UserId = userId,
                FacilityId = facilityId
            };

            _context.Reservations.Add(newReservation);
            _context.SaveChanges();

            // Add notification to staff
            var notification = new Notification
            {
                UserId = null,
                TargetRole = "Staff",
                Title = $"New {reservation.FacilityName} Facility Reservation Request",
                Message = $"There is a new facility reservation request by {personName}. Please review it.",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Type = "Facility Reservation Request",
                Link = "/staff/requests/reservation"
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            // Send a SignalR notification to all staff
            _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
            {
                Title = "New Facility Request",
                Message = $"A new facility request for {reservation.FacilityName} has been submitted by {personName}.",
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            });

            return Json(new { success = true, message = "Reservation added successfully." });
        }


        public async Task<IActionResult> Bill()
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var bills = _context.Bill.Where(b => b.UserId == userId).ToList();
            var payments = _context.Payment.Where(p => p.UserId == userId).ToList();

            if (!bills.Any())
            {
                ViewBag.Message = "No bills found.";
                return View();
            }

            var notificationsToInsert = new List<Notification>();
            var homeownerNotifications = new List<Notification>();
            var staffNotifications = new List<Notification>();
            var adminNotifications = new List<Notification>();

            var today = DateTime.Today;
            var user = _context.User_Accounts.FirstOrDefault(u => u.Id == userId);

            foreach (var bill in bills)
            {
                if (bill.Status == "Paid") continue;

                if (!DateTime.TryParse(bill.DueDate, out var dueDate))
                {
                    bill.Status = "Invalid Date";
                    continue;
                }

                var totalPaid = payments.Where(p => p.BillId == bill.BillId).Sum(p => p.AmountPaid);
                var remainingAmount = bill.BillAmount - totalPaid;

                string newStatus = bill.Status;
                string title = "", message = "";

                if (remainingAmount <= 0)
                {
                    newStatus = "Paid";
                    title = "Bill Fully Paid";
                    message = $"Thank you! Your payment for {bill.BillName} is now fully settled as of {DateTime.Now:MMMM dd, yyyy}.";
                }
                else if (dueDate < today)
                {
                    newStatus = "Overdue";
                    title = "Bill Overdue";
                    message = $"Reminder: Your bill {bill.BillName} is overdue.";
                    if (remainingAmount > 0)
                    {
                        message += $" Please settle the remaining balance of <strong>₱{remainingAmount:N2}</strong> as soon as possible.";
                    }
                }
                else if (dueDate == today)
                {
                    newStatus = "Due Now";
                    title = "Bill Due Today";
                    message = $"Heads up! Your bill {bill.BillName} is due today.";
                    if (remainingAmount > 0)
                    {
                        message += $" Please settle the remaining balance of <strong>₱{remainingAmount:N2}</strong> as soon as possible.";
                    }
                }
                else
                {
                    newStatus = "Upcoming";
                    title = "Upcoming Bill";
                    message = $"Your bill {bill.BillName} is due on {dueDate:MMMM dd}. Remaining balance: ₱{remainingAmount:N2}.";
                }

                // Format full name for Admin/Staff
                var fullName = user != null
                    ? $"{char.ToUpper(user.Firstname[0]) + user.Firstname.Substring(1).ToLower()} {char.ToUpper(user.Lastname[0]) + user.Lastname.Substring(1).ToLower()}"
                    : $"User #{userId}";

                // Format deadline date
                var deadlineDate = newStatus == "Due Now" ? today.ToString("MMMM dd, yyyy") : dueDate.ToString("MMMM dd, yyyy");

                // Common message for Admin/Staff
                string formattedMessage = $"The <strong>{bill.BillName}</strong> Bill of <strong>{fullName}</strong> is now <strong>{newStatus}</strong> that is set to deadline on <strong>{deadlineDate}</strong>.";

                if (bill.Status != newStatus)
                {
                    bill.Status = newStatus;
                    _context.SaveChanges(); // Save bill status change

                    var now = DateTime.UtcNow;

                    // Homeowner
                    homeownerNotifications.Add(new Notification
                    {
                        UserId = userId,
                        Message = message,
                        Title = title,
                        DateCreated = now,
                        IsRead = false,
                        TargetRole = "Homeowner",
                        Link = "/home/bill",
                        Type = "Payment"
                    });

                    // Staff
                    staffNotifications.Add(new Notification
                    {
                        UserId = userId,
                        Message = formattedMessage,
                        Type = "Payment",
                        Title = title,
                        DateCreated = now,
                        IsRead = false,
                        TargetRole = "Staff",
                        Link = "/staff/bills_and_payments"
                    });

                    // Admin
                    adminNotifications.Add(new Notification
                    {
                        UserId = userId,
                        Message = formattedMessage,
                        Type = "Payment",
                        Title = title,
                        DateCreated = now,
                        IsRead = false,
                        TargetRole = "Admin",
                        Link = "/admin/paymenthistory"
                    });
                }
            }

            // Bulk insert notifications
            _context.Notifications.AddRange(homeownerNotifications);
            _context.Notifications.AddRange(staffNotifications);
            _context.Notifications.AddRange(adminNotifications);
            _context.SaveChanges();

            // SignalR notify each group
            foreach (var note in homeownerNotifications)
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", note);
            }

            foreach (var note in staffNotifications)
            {
                await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", note);
            }

            foreach (var note in adminNotifications)
            {
                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", note);
            }

            var outstandingBills = bills
     .Where(b => b.Status == "Overdue" || b.Status == "Due Now")
     .Select(b => new
     {
         b.BillId,
         b.BillName,
         b.DueDate,
         RemainingAmount = b.BillAmount - payments.Where(p => p.BillId == b.BillId).Sum(p => p.AmountPaid)
     })
     .OrderBy(b => b.BillId)
     .ToList();

            var overdueBills = bills
                .Where(b => b.Status == "Overdue")
                .OrderBy(b => b.BillId)
                .ToList();

            var upcomingBills = bills
                .Where(b => b.Status == "Upcoming")
                .Select(b => new
                {
                    b.BillId,
                    b.BillName,
                    b.DueDate,
                    b.BillAmount,
                    RemainingAmount = b.BillAmount - payments.Where(p => p.BillId == b.BillId).Sum(p => p.AmountPaid)
                })
                .OrderBy(b => b.BillId)
                .ToList();

            var paymentHistory = (from p in payments
                                  join b in bills on p.BillId equals b.BillId
                                  select new
                                  {
                                      p.PaymentId,
                                      b.BillName,
                                      p.DatePaid,
                                      p.PaymentMethod,
                                      p.AmountPaid
                                  })
                                 .OrderByDescending(p => p.DatePaid)
                                 .ToList();

            ViewBag.OutstandingBills = outstandingBills;
            ViewBag.PaymentHistory = paymentHistory;
            ViewBag.OverdueBills = overdueBills;
            ViewBag.UpcomingBills = upcomingBills;

            return View(bills);
        }

        public async Task<IActionResult> PaymentPanel(int billId)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var bill = _context.Bill.FirstOrDefault(b => b.BillId == billId && b.UserId == userId);
            if (bill == null)
            {
                return RedirectToAction("Bill");
            }

            var payments = _context.Payment
                .Where(p => p.UserId == userId && p.BillId == bill.BillId)
                .ToList();

            var totalPaid = payments.Sum(p => p.AmountPaid);
            var remainingAmount = bill.BillAmount - totalPaid;

            string newStatus = bill.Status;
            string notifTitle = "";
            string notifMessage = "";
            bool sendNotif = false;

            // Determine new status
            if (remainingAmount <= 0)
            {
                newStatus = "Paid";
                notifTitle = "Bill Fully Paid";
                notifMessage = $"Thank you! Your payment for <strong>{bill.BillName}</strong> is now fully settled as of {DateTime.Now:MMMM dd, yyyy}.";
            }
            else if (DateTime.Parse(bill.DueDate) < DateTime.Today)
            {
                newStatus = "Overdue";
                notifTitle = "Bill Overdue";
                notifMessage = $"Reminder: Your bill <strong>{bill.BillName}</strong> is overdue.";
                notifMessage += $" Please settle the remaining balance of <strong>₱{remainingAmount:N2}</strong> as soon as possible.";
            }
            else if (DateTime.Parse(bill.DueDate) == DateTime.Today)
            {
                newStatus = "Due Now";
                notifTitle = "Bill Due Today";
                notifMessage = $"Heads up! Your bill <strong>{bill.BillName}</strong> is due today.";
                notifMessage += $" Remaining balance: <strong>₱{remainingAmount:N2}</strong>.";
            }
            else
            {
                newStatus = "Upcoming";
                notifTitle = "Upcoming Bill";
                notifMessage = $"Your bill <strong>{bill.BillName}</strong> is due on {DateTime.Parse(bill.DueDate):MMMM dd}.";
                notifMessage += $" Remaining balance: <strong>₱{remainingAmount:N2}</strong>.";
            }

            // Only proceed if status is actually changing
            if (bill.Status != newStatus)
            {
                bill.Status = newStatus;
                _context.SaveChanges();
                sendNotif = true;
            }

            if (sendNotif)
            {
                var user = _context.User_Accounts.FirstOrDefault(u => u.Id == userId);
                var fullName = user != null
                    ? $"{char.ToUpper(user.Firstname[0]) + user.Firstname[1..].ToLower()} {char.ToUpper(user.Lastname[0]) + user.Lastname[1..].ToLower()}"
                    : $"User #{userId}";

                var roles = new[] { "Admin", "Staff", "Homeowner" };
                foreach (var role in roles)
                {
                    string link = role switch
                    {
                        "Admin" => "/admin/paymenthistory",
                        "Staff" => "/staff/bills_and_payments",
                        "Homeowner" => "/home/bill",
                        _ => "/"
                    };

                    string message = notifMessage;

                    if (role == "Admin" || role == "Staff")
                    {
                        string deadlineDate = newStatus == "Due Now"
                            ? DateTime.Now.ToString("MMMM dd, yyyy")
                            : DateTime.Parse(bill.DueDate).ToString("MMMM dd, yyyy");

                        message = $"The <strong>{bill.BillName}</strong> Bill of <strong>{fullName}</strong> is now <strong>{newStatus}</strong> that is set to deadline on <strong>{deadlineDate}</strong>.";
                    }

                    var notif = new Notification
                    {
                        UserId = userId,
                        Message = message,
                        Type = "Payment",
                        Title = notifTitle,
                        DateCreated = DateTime.UtcNow,
                        IsRead = false,
                        TargetRole = role,
                        Link = link
                    };

                    _context.Notifications.Add(notif);
                    _context.SaveChanges();

                    // Send real-time
                    if (role == "Homeowner")
                    {
                        await _hubContext.Clients.User(userId.ToString())
                            .SendAsync("ReceiveNotification", new
                            {
                                Title = notifTitle,
                                Message = notifMessage,
                                DateCreated = notif.DateCreated
                            });
                    }
                    else
                    {
                        await _hubContext.Clients.Group(role.ToLower())
                            .SendAsync("ReceiveNotification", new
                            {
                                Title = notifTitle,
                                Message = message,
                                DateCreated = notif.DateCreated
                            });
                    }
                }
            }

            ViewBag.Bill = bill;
            return View();
        }

        /*
        [HttpPost]
        public async Task<IActionResult> ProcessSquarePayment([FromBody] SquarePaymentRequest request)
        {
            // Create a Square client with sandbox environment
            var client = new SquareClient.Builder()
                .Environment(Square.Environment.Sandbox)
                .AccessToken("EAAAl-M5lrKVpmG1yueFTWHNEUSBoWQLmANRsW8KGNeSrOSVE8JqITIJRQN4dele") // Replace with your actual token
                .Build();

            var paymentsApi = client.PaymentsApi;

            // Amount in centavos (smallest unit of PHP)
            var amountMoney = new Money(amount: (long)(request.Amount * 100), currency: "PHP");

            // Create payment request
            var createPaymentRequest = new CreatePaymentRequest(
                sourceId: request.Token, // Payment source token (e.g. from Square Web Payments SDK)
                idempotencyKey: Guid.NewGuid().ToString(), // Unique key for safety
                amountMoney: amountMoney
            );

            try
            {
                var response = await paymentsApi.CreatePaymentAsync(createPaymentRequest);
                return Json(new
                {
                    success = true,
                    payment = response.Payment
                });
            }
            catch (ApiException ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    details = ex.Errors
                });
            }
        }

        public class SquarePaymentRequest
        {
            public string Token { get; set; }
            public decimal Amount { get; set; }
        }
       
        // This for payment in paymongo
        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int billId, decimal amountPaid, string paymentMethod)
        {
            RefreshJwtCookies();
            try
            {
                // Create Payment Intent
                var paymentIntent = await _payMongoServices.CreatePaymentIntent(
                    amount: amountPaid,
                    description: $"Bill Payment #{billId}",
                    paymentMethods: new[] { paymentMethod.ToLower() }
                );

                // Log the raw response for debugging
                _logger.LogInformation("Payment Intent Response: {@PaymentIntent}", paymentIntent);

                if (paymentIntent?.Data?.Attributes == null)
                {
                    TempData["ErrorMessage"] = "Failed to create payment intent. Please try again.";
                    return RedirectToAction("Bill", new { id = billId });
                }

                var status = paymentIntent.Data.Attributes.Status;
                var paymentUrl = paymentIntent.Data.Attributes.NextAction?.Redirect?.Url;

                if (status == "awaiting_next_action" && !string.IsNullOrEmpty(paymentUrl))
                {
                    // Redirect to PayMongo for payment
                    return Redirect(paymentUrl);
                }
                else
                {
                    TempData["ErrorMessage"] = "Payment could not be processed. Please try again.";
                    return RedirectToAction("Bill", new { id = billId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing failed");
                TempData["ErrorMessage"] = "Payment processing failed. Please try again later.";
                return RedirectToAction("Bill", new { id = billId });
            }
        }
        */

        [HttpPost]
        public async Task<IActionResult> PaymentInsertData([FromBody] PaymentDto payment)
        {
            RefreshJwtCookies();

            try
            {
                var Iduser = HttpContext.Request.Cookies["Id"];
                if (!int.TryParse(Iduser, out int userId))
                {
                    return Json(new { success = false, message = "Invalid user session." });
                }

                var bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == payment.BillId);
                if (bill == null)
                {
                    return Json(new { success = false, message = "Bill not found." });
                }

                if (bill.Status == "Paid")
                {
                    return Json(new
                    {
                        success = false,
                        message = "This bill has already been paid. Redirecting...",
                        redirect = Url.Action("Bill", "Home")
                    });
                }

                var totalPaid = _context.Payment
                    .Where(p => p.BillId == payment.BillId && p.UserId == userId)
                    .Sum(p => p.AmountPaid);

                var datePaid = DateTime.Now;
                var formattedDatePaid = datePaid.ToString("yyyy-MM-dd");

                var newPayment = new Payments
                {
                    BillId = payment.BillId,
                    UserId = userId,
                    AmountPaid = payment.AmountPaid,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentStatus = true,
                    DatePaid = formattedDatePaid
                };

                _context.Payment.Add(newPayment);
                await _context.SaveChangesAsync();

                totalPaid += payment.AmountPaid;

                if (totalPaid >= bill.BillAmount)
                {
                    bill.Status = "Paid";
                    await _context.SaveChangesAsync();
                }

                // Create user full name for notifications
                var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Id == userId);
                var fullName = user != null
                    ? $"{char.ToUpper(user.Firstname[0]) + user.Firstname[1..].ToLower()} {char.ToUpper(user.Lastname[0]) + user.Lastname[1..].ToLower()}"
                    : $"User #{userId}";

                var formattedDatePaidMessage = datePaid.ToString("MM/dd/yyyy");
                string formattedAmount = payment.AmountPaid.ToString("C");
                string billName = bill.BillName ?? $"Bill #{payment.BillId}";

                string receiptMessage = $"""
    💳 *Payment Receipt*
    Bill Name: {billName}
    Date Paid: {formattedDatePaidMessage}
    Amount Paid: {formattedAmount}
    Payment Method: {payment.PaymentMethod}

    Thank you for your payment.
    """;

                string staffMessage = $"""
    📥 *New Payment Received*
    From: {fullName}
    Bill Name: {billName}
    Amount Paid: {formattedAmount}
    Date Paid: {formattedDatePaidMessage}
    Method: {payment.PaymentMethod}
    """;

                // Now insert notification only after successful payment and bill update
                var notifications = new List<Notification>
        {
            new Notification
            {
                UserId = userId,
                Title = $"{billName} Bill",
                Message = receiptMessage,
                Type = "Payment",
                TargetRole = "Homeowner",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Link = "/home/bill"
            },
            new Notification
            {
                UserId = null,
                Title = $"{billName} Bill",
                Message = staffMessage,
                Type = "Payment",
                TargetRole = "Staff",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Link = "/staff/bills_and_payments"
            },
            new Notification
            {
                UserId = null,
                Title = $"{billName} Bill",
                Message = staffMessage,
                Type = "Payment",
                TargetRole = "Admin",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Link = "/admin/paymenthistory"
            }
        };

                _context.Notifications.AddRange(notifications);
                await _context.SaveChangesAsync(); // Make sure this is successful before proceeding

                try
                {
                    // Send SignalR notifications
                    int successCount = 0;
                    int failureCount = 0;

                    foreach (var notif in notifications)
                    {
                        if (notif.TargetRole == "Homeowner")
                        {
                            try
                            {
                                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notif);
                                successCount++;
                            }
                            catch
                            {
                                failureCount++;
                            }
                        }
                        else
                        {
                            try
                            {
                                await _hubContext.Clients.Group(notif.TargetRole.ToLower()).SendAsync("ReceiveNotification", notif);
                                successCount++;
                            }
                            catch
                            {
                                failureCount++;
                            }
                        }
                    }

                    // Email Notification Logic
                    List<string> failedEmails = new List<string>();
                    try
                    {
                        // Send the payment receipt email to the homeowner who made the payment
                        bool emailSuccess = await SendEmailPaymentReceipt(
                            user.Email, // Send email to the homeowner who made the payment
                            fullName,
                            billName,
                            formattedAmount,
                            payment.PaymentMethod,
                            formattedDatePaidMessage
                        );

                        // Provide feedback about notifications and emails
                        string emailFeedback = emailSuccess
                            ? "Payment receipt email sent successfully."
                            : "Unable to send payment receipt email.";

                        // Provide UI feedback based on success/failure counts
                        return Json(new
                        {
                            success = true,
                            message = "Payment successful. " + emailFeedback,
                            redirect = Url.Action("Bill", "Home")
                        });
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx, "Failed to send some notifications.");
                        return Json(new
                        {
                            success = true,
                            message = "Payment successful, but some notifications failed to send.",
                            redirect = Url.Action("Bill", "Home")
                        });
                    }
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, "Failed to send some notifications.");
                    return Json(new
                    {
                        success = true,
                        message = "Payment successful, but some notifications failed to send.",
                        redirect = Url.Action("Bill", "Home")
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inserting payment");
                return Json(new { success = false, message = "Error processing payment." });
            }
        }

        private async Task<bool> SendEmailPaymentReceipt(string recipientEmail, string fullname, string billName, string amountPaid, string paymentMethod, string paymentDate)
        {
            try
            {
                var smtpClient = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new NetworkCredential("subvihousesubdivision@gmail.com", "tavxjmokgbjiuaco"),
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false
                };

                var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "subvi-logo.png");
                var websiteUrl = "https://subvi.com";

                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 0; padding: 0; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: #f7fafc; }}
        .header {{ background: linear-gradient(135deg, #4299e1, #3182ce); padding: 2rem; text-align: center; }}
        .content {{ padding: 2rem; background-color: white; }}
        .receipt-box {{ background: #ebf8ff; border-left: 4px solid #4299e1; padding: 1.5rem; border-radius: 6px; }}
        .footer {{ padding: 1.5rem; text-align: center; color: #718096; font-size: 0.9rem; }}
        .button {{ background: #4299e1; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; display: inline-block; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <img src='cid:company-logo' alt='Subvi Logo' style='height: 60px; margin-bottom: 1rem;' />
            <h1 style='color: white; margin: 0;'>Payment Receipt</h1>
        </div>

        <div class='content'>
            <h2 style='color: #2d3748;'>Hello {fullname},</h2>
            <p style='color: #4a5568; line-height: 1.6;'>We have successfully received your payment. Please find the details below:</p>

            <div class='receipt-box'>
                <h3 style='margin-top: 0; color: #2b6cb0;'>Payment Details</h3>
                <p style='color: #2d3748;'>Bill Name: {billName}</p>
                <p style='color: #2d3748;'>Amount Paid: {amountPaid}</p>
                <p style='color: #2d3748;'>Payment Method: {paymentMethod}</p>
                <p style='color: #2d3748;'>Date Paid: {paymentDate}</p>
            </div>

            <div style='text-align: center; margin-top: 2rem;'>
                <a href='{websiteUrl}/home/dashboard' class='button'
                   style='background: linear-gradient(135deg, #4299e1, #3182ce);
                          transition: transform 0.2s ease;
                          box-shadow: 0 4px 6px rgba(66, 153, 225, 0.2);'>
                    View More Details
                </a>
            </div>
        </div>

        <div class='footer'>
            <p style='margin: 0.5rem 0;'>Best regards,<br><strong>Subvi Management Team</strong></p>
            <p style='margin: 1rem 0; font-size: 0.8rem;'>This is an automated message - please do not reply directly to this email</p>
            <div style='margin-top: 1rem;'>
                <a href='{websiteUrl}' style='color: #4299e1; text-decoration: none; margin: 0 10px;'>Our Website</a>
                <a href='{websiteUrl}/contacts' style='color: #4299e1; text-decoration: none; margin: 0 10px;'>Contact Support</a>
            </div>
        </div>
    </div>
</body>
</html>";

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("subvihousesubdivision@gmail.com", "Subvi House Subdivision"),
                    Subject = $"💳 Payment Receipt: {billName}",
                    Body = emailBody,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(recipientEmail);

                // Embed the logo
                var logo = new LinkedResource(logoPath)
                {
                    ContentId = "company-logo",
                    ContentType = new System.Net.Mime.ContentType("image/png") // Fully qualified ContentType
                };

                var htmlView = AlternateView.CreateAlternateViewFromString(emailBody, null, "text/html");
                htmlView.LinkedResources.Add(logo);
                mailMessage.AlternateViews.Add(htmlView);

                smtpClient.Send(mailMessage);
                return true;
            }
            catch (SmtpException smtpEx)
            {
                Console.WriteLine($"SMTP Error: {smtpEx.StatusCode}");
                Console.WriteLine($"Details: {smtpEx.Message}");
                Console.WriteLine($"Inner Exception: {smtpEx.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send payment receipt email: {ex.Message}");
            }

            return false;
        }

        public class PaymentDto
        {
            public int BillId { get; set; }
            public decimal AmountPaid { get; set; }
            public string PaymentMethod { get; set; }
        }

        /*
        [HttpPost]
        public IActionResult ConfirmPayment(int billId, decimal amountPaid, string paymentMethod)
        {
            // Find the bill
            var bill = _context.Bill.FirstOrDefault(b => b.BillId == billId);
            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction("Bill");
            }

            // Insert payment record into PAYMENT table
            var payment = new Payment
            {
                AmountPaid = amountPaid,
                PaymentStatus = true,
                PaymentMethod = paymentMethod,
                DatePaid = DateTime.Now.ToString("yyyy-MM-dd"),
                BillId = billId,
                UserId = bill.UserId
            };
            _context.Payment.Add(payment);

            // Update bill status to "Paid"
            bill.Status = "Paid";

            // Save both payment and bill status update in one transaction
            _context.SaveChanges();

            TempData["Success"] = "Payment successful!";
            return RedirectToAction("Bill");
        }
        */

        public IActionResult Services()
        {
            RefreshJwtCookies();
            return View();
        }

        //Fetching Pending Service Req
        public IActionResult GetPendingServiceRequests()
        {
            var userId = HttpContext.Request.Cookies["Id"];  // Retrieve the userId from cookies (or session, etc.)

            if (!int.TryParse(userId, out int parsedUserId))
            {
                return BadRequest("User not authenticated.");  // Handle case where userId is not valid
            }

            var serviceRequests = _context.Service_Request
                .Where(sr => sr.Status == "Pending" && sr.UserId == parsedUserId)  // Filter by UserId
                .Join(_context.User_Accounts, sr => sr.UserId, u => u.Id, (sr, u) => new
                {
                    sr.ServiceRequestId,
                    sr.ReqType,
                    sr.Description,
                    sr.DateSubmitted,
                    sr.Status
                })
                .OrderByDescending(sr => sr.ServiceRequestId)
                .ToList();

            return Json(serviceRequests);
        }

        //Fetching the Scheduled/Rejected Status Data
        public IActionResult ServiceRequests(string status = "Scheduled")
        {
            var userIdString = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User not authenticated.");
            }

            // Materialize the query first
            var requestsRaw = _context.Service_Request
                .Where(sr => sr.Status == status && sr.UserId == userId)
                .ToList();

            // Project to anonymous type after fetching from DB
            var requests = requestsRaw
                .Select(sr => new
                {
                    ServiceRequestId = sr.ServiceRequestId,
                    RequestType = sr.ReqType,
                    Description = sr.Description ?? "", // Handle possible null
                    Status = sr.Status,
                    DateSubmitted = DateTime.TryParse(sr.DateSubmitted, out var parsedDate)
                        ? parsedDate.ToString("MM/dd/yyyy")
                        : sr.DateSubmitted,
                    ScheduleDate = sr.ScheduleDate?.ToString("MM/dd/yyyy hh:mm tt"),
                    RejectedReason = sr.RejectedReason
                })
                .OrderByDescending(sr => sr.ServiceRequestId)
                .ToList();

            return Json(requests);
        }

        //Insert Service Req Data
        [HttpPost]
        public IActionResult SubmitServiceRequest([FromBody] ServiceRequestDTO request)
        {
            if (ModelState.IsValid)
            {
                var userIdString = HttpContext.Request.Cookies["Id"];
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized("User not authenticated.");
                }

                var user = _context.User_Accounts.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                // Check for existing non-completed/rejected request
                var duplicateRequest = _context.Service_Request
                    .Where(r => r.ReqType.ToLower().Trim() == request.ServiceName.ToLower().Trim()
                             && r.UserId == userId)
                    .OrderByDescending(r => r.ServiceRequestId) // Get latest
                    .FirstOrDefault(r => r.Status != "Completed" && r.Status != "Rejected");

                if (duplicateRequest != null)
                {
                    string reqType = duplicateRequest.ReqType;
                    string status = duplicateRequest.Status;

                    switch (status)
                    {
                        case "Scheduled":
                            if (duplicateRequest.ScheduleDate != null)
                            {
                                var formattedDate = duplicateRequest.ScheduleDate?.ToString("MM/dd/yyyy 'at' hh:mm tt");
                                return Conflict(new
                                {
                                    message = $"You already have a scheduled {reqType} service request on {formattedDate}."
                                });
                            }
                            break;

                        case "Ongoing":
                            return Conflict(new
                            {
                                message = $"Your {reqType} service request is currently ongoing. Please wait until it is completed."
                            });

                        case "Pending":
                            return Conflict(new
                            {
                                message = $"You already submitted a {reqType} service request. Please wait for it to be approved."
                            });
                    }
                }

                // Capitalize helper
                string Capitalize(string input) =>
                    string.IsNullOrWhiteSpace(input) ? input : char.ToUpper(input[0]) + input.Substring(1).ToLower();

                string personName = $"{Capitalize(user.Firstname)} {Capitalize(user.Lastname)}";

                var newRequest = new Service_Request
                {
                    ReqType = request.ServiceName,
                    Description = request.Notes,
                    DateSubmitted = DateTime.Now.ToString("yyyy-MM-dd"),
                    Status = "Pending",
                    UserId = userId
                };

                _context.Service_Request.Add(newRequest);
                _context.SaveChanges();

                var notification = new Notification
                {
                    UserId = null,
                    TargetRole = "Staff",
                    Title = $"New {request.ServiceName} Request",
                    Message = $"There is a new service request submission made by {personName}. Please review it.",
                    DateCreated = DateTime.Now,
                    IsRead = false,
                    Type = "Service Request",
                    Link = "/staff/requests/services"
                };

                _context.Notifications.Add(notification);
                _context.SaveChanges();

                _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
                {
                    Title = "New Service Request",
                    Message = $"A new service request for {request.ServiceName} has been submitted by {personName}.",
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });

                return Ok(new { message = "Request submitted successfully." });
            }

            return BadRequest("Invalid data.");
        }

        public async Task<IActionResult> Forums()
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var rawPosts = await _context.Forum
                .Join(_context.User_Accounts,
                    f => f.UserId,
                    u => u.Id,
                    (f, u) => new
                    {
                        f.PostId,
                        f.Title,
                        f.Hashtag,
                        f.Content,
                        f.DatePosted,
                        f.UserId,
                        u.Profile,
                        u.Firstname,
                        u.Lastname
                    })
                .OrderByDescending(f => f.DatePosted)
                .ToListAsync();

            var posts = rawPosts
                .Select(f => new ForumPost
                {
                    PostId = f.PostId,
                    Title = char.ToUpper(f.Title[0]) + f.Title.Substring(1),
                    Hashtag = f.Hashtag ?? null,
                    Content = f.Content,
                    DatePosted = f.DatePosted,
                    UserId = f.UserId,
                    Profile = f.Profile,
                    Firstname = char.ToUpper(f.Firstname[0]) + f.Firstname.Substring(1),
                    Lastname = char.ToUpper(f.Lastname[0]) + f.Lastname.Substring(1),
                    FullName = char.ToUpper(f.Firstname[0]) + f.Firstname.Substring(1) + " " + char.ToUpper(f.Lastname[0]) + f.Lastname.Substring(1),
                    Likes = _context.Like.Count(l => l.PostId == f.PostId),
                    RepliesCount = _context.Replies.Count(r => r.PostId == f.PostId),
                    IsLiked = _context.Like.Any(l => l.PostId == f.PostId && l.UserId == userId)
                })
                .OrderByDescending(f => f.DatePosted)
                .ToList();

            return View(posts);
        }

        public IActionResult SearchDiscussions(string? query, string? mention)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var results = _context.Forum
                .Include(fp => fp.UserAccount) // Include the related UserAccount data
                .AsQueryable();

            // Handle @mention if present
            if (!string.IsNullOrWhiteSpace(mention))
            {
                string formattedMention = $"[{mention.Trim()}]";
                results = results.Where(fp => fp.Hashtag.Contains(formattedMention));
            }

            // Handle free-text query (Title or Content)
            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(fp =>
                    fp.Title.Contains(query) || fp.Content.Contains(query));
            }

            // Materialize results before calculating Likes/Replies to avoid EF function translation issues
            var rawResults = results
                .OrderByDescending(fp => fp.DatePosted)
                .ToList();

            var data = rawResults
                .Select(fp => new {
                    fp.PostId,
                    fp.Title,
                    fp.Hashtag,
                    fp.Content,
                    DatePosted = fp.DatePosted.ToString("MMMM dd, yyyy"),
                    fp.Profile,
                    Firstname = char.ToUpper(fp.Firstname[0]) + fp.Firstname.Substring(1),
                    Lastname = char.ToUpper(fp.Lastname[0]) + fp.Lastname.Substring(1),
                    FullName = char.ToUpper(fp.Firstname[0]) + fp.Firstname.Substring(1) + " " + char.ToUpper(fp.Lastname[0]) + fp.Lastname.Substring(1),
                    LikeCount = _context.Like.Count(l => l.PostId == fp.PostId),
                    RepliesDisplay = _context.Replies.Count(r => r.PostId == fp.PostId),
                    IsLiked = _context.Like.Any(l => l.PostId == fp.PostId && l.UserId == userId)
                }).ToList();

            return Json(data);
        }

        //Mention Announcement Title
        [HttpGet]
        public IActionResult GetAnnouncementTitles()
        {
            var titles = _context.Announcement.Select(a => a.Title).ToList();
            return Json(titles);
        }

        // Add a new post
        [HttpPost]
        public async Task<IActionResult> AddPost(string title, string content, string? hashtag)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
                return BadRequest("Title and Content cannot be empty.");

            if (!int.TryParse(Iduser, out int userId))
                return BadRequest("Invalid User ID.");

            try
            {
                // Get full name of the user
                var user = await _context.User_Accounts.FindAsync(userId);
                if (user == null)
                    return BadRequest("User not found.");

                string Capitalize(string name) => string.IsNullOrEmpty(name) ? "" : char.ToUpper(name[0]) + name.Substring(1).ToLower();
                string personname = $"{Capitalize(user.Firstname)} {Capitalize(user.Lastname)}";

                var newPost = new Forum
                {
                    Title = char.ToUpper(title[0]) + title.Substring(1),
                    Content = content,
                    DatePosted = DateTime.Now,
                    UserId = userId,
                    Hashtag = hashtag
                };

                _context.Forum.Add(newPost);
                await _context.SaveChangesAsync();

                // Add notification for admin
                var notification = new Notification
                {
                    UserId = null,
                    TargetRole = "Admin",
                    Title = "Post Like",
                    Message = $"There is a new post created by {personname} with Title {title}.",
                    DateCreated = DateTime.UtcNow,
                    IsRead = false,
                    Type = "Post Creation",
                    Link = "/admin/communityforum"
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // Notify admin via SignalR
                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
                {
                    Title = notification.Title,
                    Message = notification.Message,
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });

                return RedirectToAction("Forums");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding post: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the post.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return BadRequest("Invalid User ID.");
            }

            try
            {
                var user = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return Unauthorized();

                var forumPost = await _context.Forum.FirstOrDefaultAsync(f => f.PostId == postId);
                if (forumPost == null) return NotFound();

                string Capitalize(string name) => string.IsNullOrEmpty(name) ? "" : char.ToUpper(name[0]) + name.Substring(1).ToLower();
                var personName = $"{Capitalize(user.Firstname)} {Capitalize(user.Lastname)}";
                var title = forumPost.Title;

                var existingLike = await _context.Like
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (existingLike == null)
                {
                    // Add like
                    _context.Like.Add(new Like { PostId = postId, UserId = userId });
                    await _context.SaveChangesAsync();

                    // Add notification (ONLY for new likes)
                    var notification = new Notification
                    {
                        UserId = null,
                        TargetRole = "Admin",
                        Title = "Post Like",
                        Message = $"{personName} liked the post {title}.",
                        DateCreated = DateTime.UtcNow,
                        IsRead = false,
                        Type = "Post Like",
                        Link = "/admin/communityforum"
                    };

                    _context.Notifications.Add(notification);
                    await _context.SaveChangesAsync();

                    // Notify admin via SignalR
                    await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                    });
                }
                else
                {
                    // Remove like (unlike)
                    _context.Like.Remove(existingLike);
                    await _context.SaveChangesAsync();
                }

                return RedirectToAction("Forums");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error toggling like: {ex.Message}");
                return StatusCode(500, "An error occurred while toggling the like.");
            }
        }

        [HttpGet]
        public IActionResult Comments(int id, string title)
        {
            RefreshJwtCookies();
            // Fetch the post and replies based on the PostId (id)
            var post = _context.Forum
                .Where(f => f.PostId == id)
                .Join(_context.User_Accounts, f => f.UserId, u => u.Id, (f, u) => new
                {
                    f.PostId,
                    Title = char.ToUpper(f.Title[0]) + f.Title.Substring(1),
                    Hashtag = f.Hashtag ?? null,
                    f.Content,
                    f.DatePosted,
                    f.UserId,
                    Firstname = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                    Lastname = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1)
                })
                .FirstOrDefault();

            // Fetch replies and join with User_Accounts to get the FullName
            var replies = _context.Replies
                .Where(r => r.PostId == id)
                .Join(_context.User_Accounts, r => r.UserId, u => u.Id, (r, u) => new ReplyViewModel
                {
                    ReplyId = r.ReplyId,
                    Content = r.Content,
                    Date = r.Date,
                    UserId = r.UserId,
                    FullName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                    Firstname = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                    Lastname = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1)
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            // Pass the post and replies to the view
            var viewModel = new CommentsViewModel
            {
                Post = post,
                Replies = replies
            };

            return View(viewModel);
        }

        private string GetTruncatedTitle(string title)
        {
            var words = title.Split(' ');
            if (words.Length >= 5)
            {
                return string.Join("-", words.Take(5)) + "...";
            }
            return string.Join("-", words);
        }

        [HttpPost]
        public async Task<IActionResult> AddReply(int postId, string content)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            // Fetch user from USER_ACCOUNT
            var user = _context.User_Accounts.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Unauthorized();

            // Capitalize first letter of first and last name
            string Capitalize(string name) => string.IsNullOrEmpty(name) ? "" : char.ToUpper(name[0]) + name.Substring(1).ToLower();
            var personName = $"{Capitalize(user.Firstname)} {Capitalize(user.Lastname)}";

            // Get post title from FORUM table
            var forumPost = _context.Forum.FirstOrDefault(f => f.PostId == postId);
            if (forumPost == null) return NotFound();

            var title = forumPost.Title;

            // Save the reply
            var reply = new Replies
            {
                Content = content,
                Date = DateTime.Now,
                PostId = postId,
                UserId = userId
            };

            _context.Replies.Add(reply);
            await _context.SaveChangesAsync();

            // Create the notification
            var notification = new Notification
            {
                UserId = null,
                TargetRole = "Admin",
                Title = "Post Reply",
                Message = $"{personName} replied to the post {title}.",
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Type = "Post Reply",
                Link = $"/admin/comments/{postId}?title={GetTruncatedTitle(title)}"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send SignalR notification
            await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
            {
                Title = notification.Title,
                Message = notification.Message,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            });

            return RedirectToAction("Comments", new { id = postId, title = GetTruncatedTitle(title) });
        }

        public IActionResult Feedbacks()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult GetFeedbacks(string feedbackType = "")
        {
            RefreshJwtCookies();
            var feedbacks = string.IsNullOrEmpty(feedbackType)
                ? _context.Feedback
                    .OrderByDescending(f => f.DateSubmitted)
                    .AsEnumerable() // Switch to LINQ to Objects
                    .Select(f => new {
                        f.FeedbackId,
                        FeedbackType = f.FeedbackType ?? "Unknown",
                        Description = f.Description ?? string.Empty,
                        ComplaintStatus = f.ComplaintStatus ?? "None",
                        f.DateSubmitted,
                        f.UserId,
                        Rating = f.Rating ?? 0
                    })
                    .ToList()
                : _context.Feedback
                    .Where(f => f.FeedbackType == feedbackType)
                    .OrderByDescending(f => f.DateSubmitted)
                    .AsEnumerable() // Switch to LINQ to Objects
                    .Select(f => new {
                        f.FeedbackId,
                        FeedbackType = f.FeedbackType ?? "Unknown",
                        Description = f.Description ?? string.Empty,
                        ComplaintStatus = f.ComplaintStatus ?? "None",
                        f.DateSubmitted,
                        f.UserId,
                        Rating = f.Rating ?? 0
                    })
                    .ToList();

            return Json(feedbacks);
        }

        [HttpPost]
        public IActionResult AddFeedback([FromBody] FeedbackRequest request)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            if (string.IsNullOrWhiteSpace(request.FeedbackType) || string.IsNullOrWhiteSpace(request.Description))
            {
                return BadRequest("Feedback Type and Description are required.");
            }

            var feedback = new Feedback
            {
                FeedbackType = request.FeedbackType,
                Description = request.Description,
                Rating = request.FeedbackType == "Compliment" ? request.Rating : null,
                ComplaintStatus = request.FeedbackType == "Complaint" ? "Pending" : null,
                DateSubmitted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UserId = userId
            };

            _context.Feedback.Add(feedback);
            _context.SaveChanges();

            // Create dynamic message based on feedback type
            string notifTitle = $"{request.FeedbackType} Feedback";
            string notifMessage;

            switch (request.FeedbackType)
            {
                case "Compliment":
                    notifMessage = $"Homeowner gave a {request.Rating}-star rating and shared a compliment. Great job! 🎉";
                    break;
                case "Suggestion":
                    notifMessage = "Homeowner submitted a suggestion. Please take time to review it for improvements.";
                    break;
                case "Complaint":
                    notifMessage = "Homeowner submitted a complaint. Kindly investigate and take appropriate action.";
                    break;
                default:
                    notifMessage = "Homeowner submitted feedback. Please review it.";
                    break;
            }

            var notification = new Notification
            {
                UserId = null,
                TargetRole = "Staff",
                Title = notifTitle,
                Message = notifMessage,
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Type = "Feedback Submitted",
                Link = "/staff/feedbacks"
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            // Send a SignalR notification to all staff
            _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
            {
                Title = notifTitle,
                Message = notifMessage,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            });

            return Ok();
        }

        //For Complaint Feedback Panel
        public IActionResult GetFeedbackList(string type)
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var list = _context.Feedback
                .Where(f => f.UserId == userId &&
                            f.FeedbackType == type &&
                            (type != "Complaint" || f.ComplaintStatus != "Resolved"))
                .Join(_context.User_Accounts,
                      f => f.UserId,
                      u => u.Id,
                      (f, u) => new
                      {
                          f.FeedbackId,
                          f.FeedbackType,
                          f.Description,
                          f.ComplaintStatus,
                          f.DateSubmitted
                      })
                .OrderByDescending(f => f.DateSubmitted)
                .ToList();

            return Json(list);
        }

        public IActionResult GetResolvedFeedback()
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var feedbacks = (from f in _context.Feedback
                             join u in _context.User_Accounts on f.UserId equals u.Id
                             where f.UserId == userId &&
                                   f.FeedbackType == "Complaint" &&
                                   f.ComplaintStatus == "Resolved"
                             orderby f.DateSubmitted descending
                             select new
                             {
                                 f.FeedbackId,
                                 f.FeedbackType,
                                 f.Description,
                                 f.ComplaintStatus,
                                 f.DateSubmitted,
                                 FullName = (u.Firstname ?? "").Substring(0, 1).ToUpper() + (u.Firstname ?? "").Substring(1).ToLower() + " " +
                                            (u.Lastname ?? "").Substring(0, 1).ToUpper() + (u.Lastname ?? "").Substring(1).ToLower()
                             }).ToList();

            return Ok(feedbacks);
        }

        public IActionResult GetFeedbackDetails(int feedbackId)
        {
            var feedback = (from f in _context.Feedback
                            where f.FeedbackId == feedbackId
                            select new
                            {
                                f.FeedbackId,
                                f.FeedbackType,
                                f.Description,
                                f.DateSubmitted,
                                f.ComplaintStatus,
                                Rating = f.Rating
                            }).FirstOrDefault();

            if (feedback == null)
            {
                return NotFound(new { message = "Feedback not found" });
            }

            return Ok(feedback);
        }

        public IActionResult GetConversation(int feedbackId)
        {
            var convo = _context.FeedbackConversation
                .Where(c => c.FeedbackId == feedbackId)
                .Join(_context.User_Accounts,
                      c => c.UserId,
                      u => u.Id,
                      (c, u) => new
                      {
                          c.ConvoId,
                          c.FeedbackId,
                          c.SenderRole,
                          c.Message,
                          c.DateSent,
                          c.UserId,
                          FullName = (u.Firstname ?? "").Substring(0, 1).ToUpper() + (u.Firstname ?? "").Substring(1).ToLower() + " " +
                                     (u.Lastname ?? "").Substring(0, 1).ToUpper() + (u.Lastname ?? "").Substring(1).ToLower(),
                          ProfileImage = string.IsNullOrEmpty(u.Profile) ? null : u.Profile
                      })
                .OrderBy(c => c.DateSent)
                .ToList();

            return Json(convo);
        }

        public IActionResult SendMessage([FromBody] SendMessageDTO dto)
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return Unauthorized("User not authenticated.");
            }

            // Get user info
            var user = _context.User_Accounts.FirstOrDefault(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Capitalize first letters of first and last name
            string Capitalize(string input) =>
                string.IsNullOrWhiteSpace(input)
                    ? ""
                    : char.ToUpper(input[0]) + input.Substring(1).ToLower();

            var fullName = $"{Capitalize(user.Firstname)} {Capitalize(user.Lastname)}";

            var convo = new Feedback_Conversation
            {
                FeedbackId = dto.FeedbackId,
                SenderRole = "Homeowner",
                Message = dto.Message,
                DateSent = DateTime.Now,
                UserId = userId
            };
            _context.FeedbackConversation.Add(convo);
            _context.SaveChanges();

            // Construct the message using full name
            var notifMessage = $"Homeowner name {fullName} has sent a new message in Complaint Feedback.";

            var notification = new Notification
            {
                UserId = null,
                TargetRole = "Staff",
                Title = "New Feedback Message",
                Message = notifMessage,
                DateCreated = DateTime.UtcNow,
                IsRead = false,
                Type = "Feedback Message",
                Link = "/staff/feedbacks"
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            // Send a SignalR notification to all staff
            _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
            {
                Title = "Feedback Message",
                Message = notifMessage,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            });

            return Ok();
        }

        public class SendMessageDTO
        {
            public int FeedbackId { get; set; }
            public string Message { get; set; }
        }

        public IActionResult Resources()
        {
            RefreshJwtCookies();
            return View();
        }

        [HttpGet]
        public IActionResult GetTodayAnnouncements()
        {
            RefreshJwtCookies();
            var today = DateTime.Now.Date;
            var todayString = today.ToString("yyyy-MM-dd");

            var announcements = _context.Announcement
                .Where(a => a.DatePosted.StartsWith(todayString))
                .Select(a => new
                {
                    a.Title,
                    Time = DateTime.Parse(a.DatePosted).ToString("hh:mm tt")
                })
                .ToList();

            return Json(announcements);
        }

        [HttpGet]
        public IActionResult GetCommunityStats()
        {
            RefreshJwtCookies();
            var IduserStr = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(IduserStr, out int userId))
            {
                return RedirectToAction("landing");
            }

            using (var context = _context)
            {
                // Count Homeowners
                var memberCount = context.User_Accounts.Count(u => u.Role == "Homeowner");

                // Count Resolved Issues for the logged-in user
                var resolvedIssuesCount = context.Service_Request
                    .Count(r => r.UserId == userId && r.Status == "Resolved");

                return Json(new { memberCount, resolvedIssuesCount });
            }
        }

        public IActionResult Settings()
        {
            RefreshJwtCookies();
            var role = GetUserRoleFromToken();
            if (string.IsNullOrEmpty(role) || role != "Homeowner")
            {
                return RedirectToAction("Landing");
            }
            return View();
        }

        public async Task<IActionResult> GetUser()
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return Unauthorized();
            }
        ;
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

        public IActionResult Notifications()
        {
            RefreshJwtCookies();
            var role = GetUserRoleFromToken();
            if (string.IsNullOrEmpty(role) || role != "Homeowner")
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
}
