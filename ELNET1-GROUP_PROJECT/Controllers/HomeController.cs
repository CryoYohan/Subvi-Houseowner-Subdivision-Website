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
            string userIdStr = Request.Cookies["Id"];
            int userId = int.Parse(userIdStr);  // Converts the string to int

            var bills = _context.Bill.Where(b => b.UserId == userId).ToList();
            return View(bills);
        }

        public IActionResult PaymentPanel(int billId)
        {
            try
            {
                var bill = _context.Bill.FirstOrDefault(b => b.BillId == billId);
                if (bill == null)
                {
                    return NotFound();
                }
                return View(bill);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                TempData["Error"] = "Something went wrong while loading the payment panel.";
                return RedirectToAction("Bill");
            }
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

            // PayMongo GCash Integration
            if (paymentMethod == "G-Cash")
            {
                if (string.IsNullOrEmpty(GCashNumber))
                {
                    TempData["Error"] = "GCash number is required for GCash payments.";
                    return RedirectToAction("Bill");
                }

                // Call CreatePaymentIntent with the correct parameters
                var checkoutUrl = await _payMongoService.CreatePaymentIntent(amountPaid, "PHP");

                if (string.IsNullOrEmpty(checkoutUrl))
                {
                    TempData["Error"] = "Failed to create GCash payment intent.";
                    return RedirectToAction("Bill");
                }

                // Redirect user to GCash checkout URL
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
                    Title = f.Title,
                    Content = f.Content,
                    DatePosted = f.DatePosted,
                    UserId = f.UserId,
                    FullName = f.Firstname + " " + f.Lastname,
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
                    f.Title,
                    f.Content,
                    f.DatePosted,
                    u.Firstname,
                    u.Lastname,
                    FullName = u.Firstname + " " + u.Lastname
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
                    Title = title,
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
                return string.Join(" ", words.Take(5)) + "...";
            }
            return title;
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

        public IActionResult Resources()
        {
            RefreshJwtCookies();
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
