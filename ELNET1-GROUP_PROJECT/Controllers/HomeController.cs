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

namespace ELNET1_GROUP_PROJECT.Controllers
{
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly MyAppDBContext _context;
        private readonly PayMongoService _payMongoService;

        public HomeController(MyAppDBContext context, ILogger<HomeController> logger, PayMongoService payMongoService)
        {
            _context = context;
            _logger = logger;
            ViewData["Layout"] = "_AdminLayout";
            _payMongoService = payMongoService;
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
            RefreshJwtCookies();
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

            return View(); // Load the `dashboard.cshtml` view
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

        public IActionResult Facilities()
        {
            RefreshJwtCookies();
            return View();
        }

        public IActionResult Bill()
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("Login");
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
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("Login");
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
        public async Task<IActionResult> ConfirmPayment(int billId, decimal amountPaid, string paymentMethod, string GCashNumber = null)
        {
            var bill = _context.Bill.FirstOrDefault(b => b.BillId == billId);
            if (bill == null)
            {
                TempData["Error"] = "Bill not found.";
                return RedirectToAction("Bill");
            }

            var totalPaid = _context.Payment.Where(p => p.BillId == billId).Sum(p => p.AmountPaid);
            var remainingAmount = bill.BillAmount - totalPaid - amountPaid;

            if (paymentMethod == "G-Cash")
            {
                if (string.IsNullOrEmpty(GCashNumber))
                {
                    TempData["Error"] = "GCash number is required for GCash payments.";
                    return RedirectToAction("Bill");
                }

                var checkoutUrl = await _payMongoService.CreatePaymentIntent(amountPaid, "PHP");

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    TempData["Error"] = "Failed to create GCash payment intent.";
                    return RedirectToAction("Bill");
                }

                if (remainingAmount <= 0)
                {
                    bill.Status = "Paid";
                }
                _context.SaveChanges();

                return Redirect(checkoutUrl);
            }

            TempData["Error"] = "Invalid payment method.";
            return RedirectToAction("Bill");
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

        [HttpGet]
        public async Task<IActionResult> Forums()
        {
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("Login");
            }

            var rawPosts = await _context.Forum
                .Join(_context.User_Accounts,
                    f => f.UserId,
                    u => u.Id,
                    (f, u) => new
                    {
                        f.PostId,
                        f.Title,
                        f.Content,
                        f.DatePosted,
                        f.UserId,
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
                    Content = f.Content,
                    DatePosted = f.DatePosted,
                    UserId = f.UserId,
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

        [HttpGet]
        public IActionResult SearchDiscussions(string query)
        {
            var searchResults = _context.Forum
                .Where(f => f.Title.Contains(query) || f.Content.Contains(query)) // Search by Title or Content
                .Join(_context.User_Accounts, f => f.UserId, u => u.Id, (f, u) => new
                {
                    f.PostId,
                    Title = char.ToUpper(f.Title[0]) + f.Title.Substring(1),
                    f.Content,
                    f.DatePosted,
                    Firstname = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1),
                    Lastname = char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
                    FullName = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1)
                })
                .ToList();

            return Json(searchResults); // Return the results as JSON
        }

        // Add a new post
        [HttpPost]
        public async Task<IActionResult> AddPost(string title, string content)
        {
            var Iduser = HttpContext.Request.Cookies["Id"];

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
            {
                return BadRequest("Title and Content cannot be empty.");
            }

            if (!int.TryParse(Iduser, out int userId))
            {
                return BadRequest("Invalid User ID.");
            }

            try
            {
                var newPost = new Forum
                {
                    Title = char.ToUpper(title[0]) + title.Substring(1),
                    Content = content,
                    DatePosted = DateTime.Now,
                    UserId = userId  // Now properly converted to int
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
            // Fetch the post and replies based on the PostId (id)
            var post = _context.Forum
                .Where(f => f.PostId == id)
                .Join(_context.User_Accounts, f => f.UserId, u => u.Id, (f, u) => new
                {
                    f.PostId,
                    Title = char.ToUpper(f.Title[0]) + f.Title.Substring(1),
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
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("Login");
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
            var feedbacks = string.IsNullOrEmpty(feedbackType)
                ? _context.Feedback
                    .Select(f => new {
                        f.FeedbackId,
                        FeedbackType = f.FeedbackType ?? "", // Handle NULL
                        Description = f.Description ?? string.Empty, // Handle NULL
                        f.Status,
                        f.DateSubmitted,
                        f.UserId,
                        Rating = f.Rating ?? 0 // Handle NULL
                    })
                    .ToList()
                : _context.Feedback
                    .Where(f => f.FeedbackType == feedbackType)
                    .Select(f => new {
                        f.FeedbackId,
                        FeedbackType = f.FeedbackType ?? "Unknown", // Handle NULL
                        Description = f.Description ?? string.Empty,
                        f.Status,
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
            var Iduser = HttpContext.Request.Cookies["Id"];
            if (!int.TryParse(Iduser, out int userId))
            {
                return RedirectToAction("Login");
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
                Status = true,
                DateSubmitted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UserId = userId
            };

            _context.Feedback.Add(feedback);
            _context.SaveChanges();

            return Ok();
        }

        [HttpPost]
        public IActionResult ToggleFeedbackStatus(int feedbackId)
        {
            var feedback = _context.Feedback.FirstOrDefault(f => f.FeedbackId == feedbackId);
            if (feedback != null)
            {
                feedback.Status = !feedback.Status;
                _context.SaveChanges();
            }
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
            var IduserStr = HttpContext.Request.Cookies["Id"];

            using (var context = _context)
            {
                // Count Homeowners
                var memberCount = context.User_Accounts.Count(u => u.Role == "Homeowner");

                // Count Resolved Issues for the logged-in user
                var resolvedIssuesCount = context.Service_Request
                    .Count(r => r.UserId == IduserStr && r.Status == "Resolved");

                return Json(new { memberCount, resolvedIssuesCount });
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
