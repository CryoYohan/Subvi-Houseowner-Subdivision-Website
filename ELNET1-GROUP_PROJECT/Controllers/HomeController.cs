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

namespace ELNET1_GROUP_PROJECT.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly MyAppDBContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly PayMongoServices _payMongoServices;

        public HomeController(MyAppDBContext context, ILogger<HomeController> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration, PayMongoServices payMongoServices)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            ViewData["Layout"] = "_HomeLayout";
            _payMongoServices = payMongoServices;
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
        // There is a seperation controller api for this calendar

        public IActionResult Facilities()
        {
            RefreshJwtCookies();
            return View();
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
            _context.Reservations.Add(new Reservation
            {
                SchedDate = DateOnly.FromDateTime(DateTime.Parse(reservation.SelectedDate)),
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                Status = "Pending",
                UserId = userId,
                FacilityId = facilityId
            });

            _context.SaveChanges();

            return Json(new { success = true, message = "Reservation added successfully." });
        }

        public IActionResult Bill()
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

            foreach (var bill in bills)
            {
                if (bill.Status != "Paid")
                {
                    if (!DateTime.TryParse(bill.DueDate, out var dueDate))
                    {
                        // Handle invalid date format
                        bill.Status = "Invalid Date";
                        continue;
                    }

                    var today = DateTime.Today;
                    var totalPaid = payments.Where(p => p.BillId == bill.BillId).Sum(p => p.AmountPaid);
                    var remainingAmount = bill.BillAmount - totalPaid;

                    if (remainingAmount <= 0)
                    {
                        bill.Status = "Paid";
                    }
                    else if (dueDate < today)
                    {
                        bill.Status = "Overdue";
                    }
                    else if (dueDate == today)
                    {
                        bill.Status = "Due Now";
                    }
                    else
                    {
                        bill.Status = "Upcoming";
                    }
                }
            }

            _context.SaveChanges();

            var outstandingBills = bills
                .Where(b => b.Status == "Overdue" || b.Status == "Due Now")
                .Select(b => new
                {
                    b.BillId,
                    b.BillName,
                    b.DueDate,
                    RemainingAmount = b.BillAmount - payments.Where(p => p.BillId == b.BillId).Sum(p => p.AmountPaid)
                })
                .ToList();
            var overdueBills = bills.Where(b => b.Status == "Overdue").ToList();
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
                      .ToList();

            ViewBag.OutstandingBills = outstandingBills;
            ViewBag.PaymentHistory = paymentHistory;
            ViewBag.OverdueBills = overdueBills;
            ViewBag.UpcomingBills = upcomingBills;

            return View(bills);
        }

        public IActionResult PaymentPanel(int billId)
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
                return RedirectToAction("Bill"); // Redirect to a general bill page if not found
            }

            var payments = _context.Payment.Where(p => p.UserId == userId && p.BillId == bill.BillId).ToList();

            var totalPaid = payments.Sum(p => p.AmountPaid);
            var remainingAmount = bill.BillAmount - totalPaid;

            if (remainingAmount <= 0)
            {
                bill.Status = "Paid";
            }
            else if (DateTime.Parse(bill.DueDate) < DateTime.Today)
            {
                bill.Status = "Overdue";
            }
            else if (DateTime.Parse(bill.DueDate) == DateTime.Today)
            {
                bill.Status = "Due Now";
            }
            else
            {
                bill.Status = "Upcoming";
            }

            _context.SaveChanges();

            ViewBag.Bill = bill;  

            return View();
        }

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

        [HttpPost]
        public async Task<IActionResult> PaymentCallback(int billId, decimal amountPaid, int userId)
        {
            RefreshJwtCookies();
            try
            {
                // Get the total payments made for this bill by this user
                var totalPaid = _context.Payment
                    .Where(p => p.BillId == billId && p.UserId == userId)
                    .Sum(p => p.AmountPaid);

                // Get Bill Amount
                var bill = await _context.Bill.FirstOrDefaultAsync(b => b.BillId == billId);

                if (bill == null)
                {
                    TempData["ErrorMessage"] = "Bill not found.";
                    return RedirectToAction("Bill", new { id = billId });
                }

                // Insert the new payment
                var newPayment = new Payment
                {
                    BillId = billId,
                    UserId = userId,
                    AmountPaid = amountPaid,
                    DatePaid = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                _context.Payment.Add(newPayment);
                await _context.SaveChangesAsync();

                // Calculate new total paid
                totalPaid += amountPaid;

                // Check if the bill is fully paid
                if (totalPaid >= bill.BillAmount)
                {
                    bill.Status = "Paid";
                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Payment successful.";
                return RedirectToAction("Bill", new { id = billId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment callback");
                TempData["ErrorMessage"] = "Error processing payment.";
                return RedirectToAction("Bill", new { id = billId });
            }
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
                .ToList();

            return Json(serviceRequests);
        }

        [HttpGet]
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
                return RedirectToAction("Forums");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding post: {ex.Message}");
                return StatusCode(500, "An error occurred while adding the post.");
            }
        }

        // Handle likes
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
                var existingLike = await _context.Like
                    .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

                if (existingLike == null)
                {
                    // Add like if it doesn't exist
                    _context.Like.Add(new Like { PostId = postId, UserId = userId });
                }
                else
                {
                    // Remove like if it already exists (unlike)
                    _context.Like.Remove(existingLike);
                }

                await _context.SaveChangesAsync();
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

        // Add a reply
        [HttpPost]
        public async Task<IActionResult> AddReply(int postId, string content)
        {
            RefreshJwtCookies();
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("landing");
            }

            var reply = new Replies
            {
                Content = content,
                Date = DateTime.Now,
                PostId = postId,
                UserId = userId
            };

            _context.Replies.Add(reply);
            await _context.SaveChangesAsync();

            return RedirectToAction("Comments", new { id = postId, title = GetTruncatedTitle(_context.Forum.First(f => f.PostId == postId).Title) });
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
                ComplaintStatus = request.FeedbackType == "Complaint" ? "PENDING" : null,
                DateSubmitted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UserId = userId
            };

            _context.Feedback.Add(feedback);
            _context.SaveChanges();

            return Ok();
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
