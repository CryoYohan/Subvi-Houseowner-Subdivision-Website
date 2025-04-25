using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ELNET1_GROUP_PROJECT.Controllers;
using ELNET1_GROUP_PROJECT.SignalR;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Net.Mail;
using System.Net;

[Route("staff")]
public class StaffController : Controller
{
    private readonly MyAppDBContext _context;
    private readonly ILogger<HomeController> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public StaffController(MyAppDBContext context, ILogger<HomeController> logger, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _logger = logger;
        ViewData["Layout"] = "_StaffLayout";
        _hubContext = hubContext;
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

    [Route("communityforum")]
    public async Task<IActionResult> CommunityForum()
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
                (f, u) => new { f, u })
            .Join(_context.User_Info,
                fu => fu.u.Id,
                ui => ui.UserAccountId,
                (fu, ui) => new
                {
                    fu.f.PostId,
                    fu.f.Title,
                    fu.f.Hashtag,
                    fu.f.Content,
                    fu.f.DatePosted,
                    fu.f.UserId,
                    ui.Profile,
                    ui.Firstname,
                    ui.Lastname,
                    fu.u.Role
                })
            .OrderByDescending(p => p.DatePosted)
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
                Role = f.Role,  
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

    [Route ("searchdiscussions")]
    public IActionResult SearchDiscussions(string? query, string? mention)
    {
        RefreshJwtCookies();
        var Iduser = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(Iduser, out int userId))
        {
            return RedirectToAction("landing");
        }

        var results = _context.Forum
            .Join(_context.User_Accounts, 
                forum => forum.UserId, 
                user => user.Id,             
                (forum, user) => new          
                {
                    forum.PostId,
                    forum.Title,
                    forum.Content,
                    forum.DatePosted,
                    forum.Hashtag,
                    forum.UserId,
                    user.Role,                
                    forum.UserAccount,        
                })
            .Join(_context.User_Info,        
                forumUser => forumUser.UserId,   
                info => info.UserAccountId,  
                (forumUser, info) => new          
                {
                    forumUser.PostId,
                    forumUser.Title,
                    forumUser.Content,
                    forumUser.DatePosted,
                    forumUser.Hashtag,
                    forumUser.UserId,
                    forumUser.UserAccount,      
                    forumUser.Role,              
                    UserInfo = info             
                })
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
                fp.Role,
                DatePosted = fp.DatePosted.ToString("MMMM dd, yyyy"),
                fp.UserInfo.Profile,
                Firstname = char.ToUpper(fp.UserInfo.Firstname[0]) + fp.UserInfo.Firstname.Substring(1),
                Lastname = char.ToUpper(fp.UserInfo.Lastname[0]) + fp.UserInfo.Lastname.Substring(1),
                FullName = char.ToUpper(fp.UserInfo.Firstname[0]) + fp.UserInfo.Firstname.Substring(1) + " " +
                           char.ToUpper(fp.UserInfo.Lastname[0]) + fp.UserInfo.Lastname.Substring(1),
                LikeCount = _context.Like.Count(l => l.PostId == fp.PostId),
                RepliesDisplay = _context.Replies.Count(r => r.PostId == fp.PostId),
                IsLiked = _context.Like.Any(l => l.PostId == fp.PostId && l.UserId == userId)
            }).ToList();

        return Json(data);
    }

    //Mention Announcement Title
    [HttpGet("getannouncementtitles")]
    public IActionResult GetAnnouncementTitles()
    {
        var titles = _context.Announcement.Select(a => a.Title).ToList();
        return Json(titles);
    }

    // Add a new post
    [HttpPost("addpost")]
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
            return RedirectToAction("CommunityForum");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error adding post: {ex.Message}");
            return StatusCode(500, "An error occurred while adding the post.");
        }
    }

    [HttpPost("togglelike")]
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
            var user = await _context.User_Accounts
                .Where(u => u.Id == userId)
                .Join(_context.User_Info,
                      u => u.Id,
                      ui => ui.UserAccountId,
                      (u, ui) => new { u, ui })
                .DefaultIfEmpty()
                .Select(x => new
                {
                    x.u.Id,
                    x.u.Email,
                    x.u.Role,
                    x.ui.Firstname,
                    x.ui.Lastname,
                    x.ui.Profile,
                    x.ui.PhoneNumber
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound();

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
            }
            else
            {
                // Remove like (unlike)
                _context.Like.Remove(existingLike);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("CommunityForum");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error toggling like: {ex.Message}");
            return StatusCode(500, "An error occurred while toggling the like.");
        }
    }

    [HttpGet("comments")]
    public async Task<IActionResult> Comments(int id, string title)
    {
        RefreshJwtCookies();

        var post = await (
            from f in _context.Forum
            join u in _context.User_Accounts on f.UserId equals u.Id
            join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoJoin
            from ui in userInfoJoin.DefaultIfEmpty()
            where f.PostId == id
            select new
            {
                f.PostId,
                Title = !string.IsNullOrEmpty(f.Title) ? char.ToUpper(f.Title[0]) + f.Title.Substring(1) : "",
                f.Hashtag,
                f.Content,
                f.DatePosted,
                f.UserId,
                u.Role,
                Profile = ui != null ? ui.Profile : "",
                FullName = ui != null && !string.IsNullOrEmpty(ui.Firstname) ? char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) : "" +
                           ui != null && !string.IsNullOrEmpty(ui.Lastname) ? char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1) : "",
                Firstname = ui != null && !string.IsNullOrEmpty(ui.Firstname) ? char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) : "",
                Lastname = ui != null && !string.IsNullOrEmpty(ui.Lastname) ? char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1) : ""
            }
        ).FirstOrDefaultAsync(); 

        var replies = await (from r in _context.Replies
                             join u in _context.User_Accounts on r.UserId equals u.Id
                             join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoJoin
                             from ui in userInfoJoin.DefaultIfEmpty()
                             where r.PostId == id
                             select new ReplyViewModel
                             {
                                 ReplyId = r.ReplyId,
                                 Content = r.Content,
                                 Date = r.Date,
                                 UserId = r.UserId,
                                 Profile = ui.Profile,
                                 Role = u.Role,
                                 FullName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                                 Firstname = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1),
                                 Lastname = char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1)
                             })
            .OrderByDescending(r => r.Date)
            .ToListAsync(); 

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

    [HttpPost("addreply")]
    public async Task<IActionResult> AddReply(int postId, string content)
    {
        RefreshJwtCookies();
        var Iduser = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(Iduser, out int userId))
        {
            return RedirectToAction("landing");
        }

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

        return RedirectToAction("Comments", new { id = postId, title = GetTruncatedTitle(title) });
    }

    [Route("useraccounts")]
    public IActionResult UserAccounts()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }

        // Perform LEFT JOIN to combine USER_ACCOUNT with USER_INFO
        var users = (from ua in _context.User_Accounts
                     join ui in _context.User_Info on ua.Id equals ui.UserAccountId into joined
                     from ui in joined.DefaultIfEmpty()
                     where ua.Role != "Admin"
                     select new UserDataRequest
                     {
                         Id = ua.Id,
                         Email = ua.Email,
                         Role = ua.Role,
                         Status = ua.Status,
                         DateRegistered = ua.DateRegistered,
                         Firstname = ui != null ? ui.Firstname : null,
                         Lastname = ui != null ? ui.Lastname : null,
                         Address = ui != null ? ui.Address : null,
                         PhoneNumber = ui != null ? ui.PhoneNumber : null,
                         Profile = ui != null ? ui.Profile : null,
                         DateCreated = ui.DateCreated
                     }).ToList();

        return View(users);
    }

    //For resetting password
    [HttpPost("user/resetpassword")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = _context.User_Accounts.FirstOrDefault(u => u.Id == id);
        if (user == null)
            return NotFound(new { message = "User not found." });

        var info = (from account in _context.User_Accounts
                    join userInfo in _context.User_Info
                        on account.Id equals userInfo.PersonId into joined
                    from userInfo in joined.DefaultIfEmpty()
                    where account.Id == id
                    select new
                    {
                        Account = account,
                        Info = userInfo // this can be null if there's no match
                    }).FirstOrDefault();

        if (info == null)
            return NotFound(new { message = "User info not found." });

        // Find the maximum number of digits among all IDs
        int maxDigits = _context.User_Accounts.Max(u => u.Id.ToString().Length);
        int paddedLength = Math.Max(6, maxDigits); // minimum 6 digits

        // Generate password like Subvi-000021 (or longer if needed)
        string paddedId = id.ToString().PadLeft(paddedLength, '0');
        string plainPassword = $"subvi-{paddedId}";

        // Hash and update
        user.Password = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        _context.SaveChanges();

        // Create a notification
        var notification = new Notification
        {
            UserId = id,
            TargetRole = "Homeowner",
            Type = "Visitor",
            Title = "Visitor Status Updated to Active",
            Message = $"Good Day! We want you to know that your password has been reset as of {DateTime.Now:MM/dd/yyyy}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.User(id.ToString()).SendAsync("ReceiveNotification", notification);

        // Send email
        string fullname = info.Info != null
                ? $"{CultureInfo.CurrentCulture.TextInfo.ToTitleCase(info.Info.Firstname.ToLower())} {CultureInfo.CurrentCulture.TextInfo.ToTitleCase(info.Info.Lastname.ToLower())}"
                : "Valued User";
        bool sent = SendResetPasswordEmail(user.Email, fullname, plainPassword);

        if (!sent)
        {
            return Ok(new { message = "Password reset successfully, but failed to send email." });
        }

        return Ok(new { message = "Password has been reset and email sent successfully." });
    }

    private bool SendResetPasswordEmail(string recipientEmail, string fullname, string newPassword)
    {
        try
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential("subvihousesubdivision@gmail.com", "kiccqgjvvxwiypcn"),
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
                    .credentials {{ background: #f1f5f9; border-radius: 8px; padding: 1.5rem; margin: 1rem 0; }}
                    .footer {{ padding: 1.5rem; text-align: center; color: #718096; font-size: 0.9rem; }}
                    .button {{ background: #4299e1; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; display: inline-block; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <img src='cid:company-logo' alt='Subvi Logo' style='height: 60px; margin-bottom: 1rem;' />
                        <h1 style='color: white; margin: 0;'>Password Reset Successful</h1>
                    </div>
            
                    <div class='content'>
                        <h2 style='color: #2d3748;'>Hello, {fullname}!</h2>
                        <p style='color: #4a5568; line-height: 1.6;'>
                            Your account password has been successfully reset. Here are your updated login credentials:
                        </p>

                        <div class='credentials'>
                            <p><strong>Email:</strong> <span style='color: #2d3748;'>{recipientEmail}</span></p>
                            <p><strong>New Password:</strong> <span style='color: #2d3748;'>{newPassword}</span></p>
                        </div>

                        <p style='color: #4a5568;'>We recommend changing your password after logging in for your security.</p>

                        <div style='text-align: center; margin: 2rem 0;'>
                            <a href='{websiteUrl}/login' class='button'>
                                Login to Your Account
                            </a>
                        </div>
                    </div>

                    <div class='footer'>
                        <p>Regards,</p>
                        <p><strong>Subvi Management Team</strong></p>
                        <p style='font-size: 0.8rem;'>This is an automated message - please do not reply</p>
                    </div>
                </div>
            </body>
            </html>";

            var mailMessage = new MailMessage
            {
                From = new MailAddress("subvihousesubdivision@gmail.com", "Subvi House Subdivision"),
                Subject = "Your Subvi Account Password Has Been Reset",
                Body = emailBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(recipientEmail);

            var logo = new LinkedResource(logoPath)
            {
                ContentId = "company-logo",
                ContentType = new System.Net.Mime.ContentType("image/png")
            };

            var htmlView = AlternateView.CreateAlternateViewFromString(emailBody, null, "text/html");
            htmlView.LinkedResources.Add(logo);
            mailMessage.AlternateViews.Add(htmlView);

            smtpClient.Send(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Email send error: {ex.Message}");
            return false;
        }
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
        var profilePath = (from account in _context.User_Accounts
                           join info in _context.User_Info
                           on account.Id equals info.UserAccountId into joined
                           from info in joined.DefaultIfEmpty()
                           where account.Id == userId
                           select info.Profile).FirstOrDefault();

        ViewBag.ProfilePath = profilePath;

        return View();
    }

    //For fetching the counting of pending in reservation
    [HttpGet("facility/reservation/pending-count")]
    public async Task<IActionResult> GetPendingReservationCount()
    {
        var count = await _context.Reservations
            .CountAsync(r => r.Status == "Pending");

        return Ok(new { count });
    }

    //For fetching the counting of pending in service request
    [HttpGet("servicerequest/pending-count")]
    public async Task<IActionResult> GetPendingServiceRequestCount()
    {
        var count = await _context.Service_Request
            .Where(r => r.Status == "Pending")
            .CountAsync();

        return Ok(new { count });
    }

    //For all announcements to fetch
    [HttpGet("announcement/all")]
    public async Task<IActionResult> GetAll()
    {
        var announcements = await _context.Announcement
            .OrderByDescending(a => a.DatePosted)
            .Select(a => new {
                a.Title,
                a.Description,
                a.DatePosted
            }).ToListAsync();

        return Ok(announcements);
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
        var visitors = (from visitor in _context.Visitor_Pass
                        join account in _context.User_Accounts
                            on visitor.UserId equals account.Id
                        join info in _context.User_Info
                            on account.Id equals info.UserAccountId into infoJoin
                        from info in infoJoin.DefaultIfEmpty()
                        where status == null || visitor.Status == status
                        orderby visitor.DateTime descending
                        select new
                        {
                            visitor.VisitorId,
                            VisitorName = char.ToUpper(visitor.VisitorName[0]) + visitor.VisitorName.Substring(1),
                            visitor.DateTime,
                            visitor.Relationship,
                            visitor.Status,
                            FullName = info != null
                                ? char.ToUpper(info.Firstname[0]) + info.Firstname.Substring(1) + " " +
                                  char.ToUpper(info.Lastname[0]) + info.Lastname.Substring(1)
                                : "No Name"
                        }).ToList();

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
            .GroupJoin(_context.User_Info,
                u => u.Id,
                ui => ui.UserAccountId,   
                (u, uiGroup) => new
                {
                    UserId = u.Id,
                    FirstName = uiGroup.FirstOrDefault() != null ? char.ToUpper(uiGroup.FirstOrDefault().Firstname[0]) + uiGroup.FirstOrDefault().Firstname.Substring(1) : null,
                    LastName = uiGroup.FirstOrDefault() != null ? char.ToUpper(uiGroup.FirstOrDefault().Lastname[0]) + uiGroup.FirstOrDefault().Lastname.Substring(1) : null,
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
                       join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoGroup
                       from ui in userInfoGroup.DefaultIfEmpty()
                       where v.VisitorId == id
                       select new
                       {
                           v.VisitorId,
                           v.UserId,
                           FirstName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1),
                           LastName = char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                           Email = u.Email ,
                           HomeownerName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
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
    public async Task<IActionResult> AddVisitor(int? visitorId, int userId, string visitorName, string relationship)
    {
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
            return Json(new { success = false, message = "Visitor name already registered!" });
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

        // Create a new notification
        var notification = new Notification
        {
            UserId = userId,
            TargetRole = "Homeowner",
            Type = "Visitor",
            Title = "Visitor Registered",
            Message = $"A new visitor named {trimmedVisitorName} has been registered to you as of today {DateTime.Now.ToString("MM/dd/yyyy")}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        // Add the notification to the context
        _context.Notifications.Add(notification);

        // Save changes asynchronously
        await _context.SaveChangesAsync();

        // Send real-time notification to the user
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Json(new { success = true });
    }

    [HttpPost("editvisitor")]
    public async Task<IActionResult> EditVisitor(int visitorId, int userId, string visitorName, string relationship)
    {
        // Fetch the existing visitor data
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

        // Get the current userId of the visitor before updating
        int originalUserId = visitor.UserId;

        // Update visitor data
        visitor.UserId = userId;
        visitor.VisitorName = trimmedVisitorName;
        visitor.Relationship = relationship;
        _context.SaveChanges();

        // Create a notification for the original user (if userId changed)
        var originalUserNotification = new Notification
        {
            UserId = originalUserId,
            TargetRole = "Homeowner",
            Type = "Visitor",
            Title = $"Visitor Registration Info Updated",
            Message = $"The visitor registration info that is registered to you has been updated as of today {DateTime.Now:MM/dd/yyyy}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        // Create a notification for the new user (if userId changed)
        Notification newUserNotification = null;
        if (userId != originalUserId)
        {
            newUserNotification = new Notification
            {
                UserId = userId,
                TargetRole = "Homeowner",
                Type = "Visitor",
                Title = $"Visitor Registration Info Updated",
                Message = $"The visitor registration that is now registered to you has been updated as of today {DateTime.Now:MM/dd/yyyy}.",
                IsRead = false,
                DateCreated = DateTime.Now
            };
        }

        // Add the notifications to the context
        _context.Notifications.Add(originalUserNotification);
        if (newUserNotification != null)
        {
            _context.Notifications.Add(newUserNotification);
        }

        // Save changes asynchronously
        await _context.SaveChangesAsync();

        // Send real-time notifications to both users (if userId changed)
        await _hubContext.Clients.User(originalUserId.ToString()).SendAsync("ReceiveNotification", originalUserNotification);
        if (newUserNotification != null)
        {
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", newUserNotification);
        }

        return Json(new { success = true });
    }

    //Soft Delete Visitor
    [HttpPost("deletevisitor/{id}")]
    public async Task<IActionResult> DeleteVisitor(int id)
    {
        // Fetch the visitor data to get the associated userId
        var visitor = _context.Visitor_Pass.Find(id);
        if (visitor != null)
        {
            // Get the userId from the visitor data
            int userId = visitor.UserId;

            // Mark visitor as deleted
            visitor.Status = "Deleted";
            _context.SaveChanges();

            // Create a new notification for the user
            var notification = new Notification
            {
                UserId = userId,
                TargetRole = "Homeowner",
                Type = "Visitor",
                Title = "Visitor Registration Deleted",
                Message = $"The visitor registration that has been registered to you has been deleted as of today {DateTime.Now:MM/dd/yyyy}.",
                IsRead = false,
                DateCreated = DateTime.Now
            };

            // Add the notification to the context
            _context.Notifications.Add(notification);

            // Save changes asynchronously
            await _context.SaveChangesAsync();

            // Send real-time notification to the user
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

            return Json(new { success = true });
        }

        // If visitor not found, return an error message
        return Json(new { success = false, message = "Visitor not found." });
    }

    //Setting the Visitor to Active
    [HttpPost("activatevisitor/{visitorId}")]
    public async Task<IActionResult> SetVisitorActive(int visitorId)
    {
        var visitor = await _context.Visitor_Pass.FindAsync(visitorId);
        if (visitor == null) return NotFound();

        visitor.Status = "Active";
        _context.Visitor_Pass.Update(visitor);

        // Get userId from visitor record
        var userId = visitor.UserId;

        // Create a notification for setting visitor to Active
        var notification = new Notification
        {
            UserId = userId,
            TargetRole = "Homeowner",
            Type = "Visitor",
            Title = "Visitor Status Updated to Active",
            Message = $"The status of your visitor {visitor.VisitorName} has been now updated back to active as of {DateTime.Now:MM/dd/yyyy}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { success = true });
    }

    //Prohibit the Visitor to enter by setting status to Prohibited
    [HttpPost("restrictvisitor/{visitorId}")]
    public async Task<IActionResult> SetVisitorRestricted(int visitorId)
    {
        var visitor = await _context.Visitor_Pass.FindAsync(visitorId);
        if (visitor == null) return NotFound();

        visitor.Status = "Prohibited";
        _context.Visitor_Pass.Update(visitor);

        // Get userId from visitor record
        var userId = visitor.UserId;

        // Create a notification for restricting visitor
        var notification = new Notification
        {
            UserId = userId,
            TargetRole = "Homeowner",
            Type = "Visitor",
            Title = "Visitor Status Updated",
            Message = $"Your visitor {visitor.VisitorName} has been prohibited from entering the premises effective {DateTime.Now:MM/dd/yyyy}. If you have any concern you can contact us anytime.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { success = true });
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
                       join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoGroup
                       from ui in userInfoGroup.DefaultIfEmpty()
                       where v.VehicleId == id
                       select new
                       {
                           v.VehicleId,
                           v.PlateNumber,
                           v.Type,
                           v.Status,
                           v.Color,
                           v.VehicleBrand,
                           v.UserId,
                           FirstName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1),
                           LastName = char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                           Email = u.Email,
                           HomeownerName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                       }).FirstOrDefault();

        if (vehicle == null)
        {
            return NotFound();
        }

        return Ok(vehicle);
    }

    [HttpPost("VehicleRegistration")]
    public async Task<IActionResult> AddVehicle([FromBody] VehicleRegistration vehicle)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        bool conflictExists = _context.Vehicle_Registration.Any(v =>
            v.PlateNumber == vehicle.PlateNumber &&
            v.Type == vehicle.Type &&
            v.Color == vehicle.Color &&
            v.VehicleBrand == vehicle.VehicleBrand
        );

        if (conflictExists)
        {
            return Conflict("The car type submitted already registered.");
        }

        _context.Vehicle_Registration.Add(vehicle);
        _context.SaveChanges();

        var notification = new Notification
        {
            UserId = vehicle.UserId,
            TargetRole = "Homeowner",
            Type = "Vehicle",
            Title = "Vehicle Registered",
            Message = $"A new vehicle has been registered as of today {DateTime.Now:MM/dd/yyyy}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        await _hubContext.Clients.User(vehicle.UserId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(vehicle);
    }

    [HttpPut("VehicleRegistration/{id}")]
    public async Task<IActionResult> UpdateVehicle(int id, [FromBody] VehicleRegistration updated)
    {
        if (id != updated.VehicleId) return BadRequest();

        var vehicle = _context.Vehicle_Registration.FirstOrDefault(v => v.VehicleId == id);
        if (vehicle == null) return NotFound();

        bool conflictExists = _context.Vehicle_Registration.Any(v =>
            v.VehicleId != id && // not the same record
            v.PlateNumber == updated.PlateNumber &&
            v.Type == updated.Type &&
            v.Color == updated.Color &&
            v.VehicleBrand == updated.VehicleBrand
        );

        if (conflictExists)
        {
            return Conflict("A vehicle with the same information already registered to someone else.");
        }

        // Fetch current userId from the existing vehicle record
        int currentUserId = vehicle.UserId;

        // Update vehicle details
        vehicle.PlateNumber = updated.PlateNumber;
        vehicle.Type = updated.Type;
        vehicle.Status = updated.Status;
        vehicle.Color = updated.Color;
        vehicle.VehicleBrand = updated.VehicleBrand;
        vehicle.UserId = updated.UserId; // Set new userId

        _context.SaveChanges();

        string maskedPlateNumber = MaskPlateNumber(vehicle.PlateNumber);

        // Notification logic
        if (currentUserId != updated.UserId) // Check if userId has changed
        {
            // Notification for the old user (homeowner whose vehicle info has been updated)
            var oldUserNotification = new Notification
            {
                UserId = currentUserId,
                TargetRole = "Homeowner",
                Type = "Vehicle",
                Title = "Vehicle Registration Updated",
                Message = $"The vehicle registration information for your vehicle {maskedPlateNumber} has been updated.",
                IsRead = false,
                DateCreated = DateTime.Now
            };
            _context.Notifications.Add(oldUserNotification);

            // Notification for the new user (new homeowner associated with the updated vehicle)
            var newUserNotification = new Notification
            {
                UserId = updated.UserId,
                TargetRole = "Homeowner",
                Type = "Vehicle",
                Title = "Vehicle Registration Updated",
                Message = $"A vehicle registration information update has been made for the vehicle {maskedPlateNumber}.",
                IsRead = false,
                DateCreated = DateTime.Now
            };
            _context.Notifications.Add(newUserNotification);

            // Save changes asynchronously
            await _context.SaveChangesAsync();

            // Send real-time notifications
            await _hubContext.Clients.User(currentUserId.ToString()).SendAsync("ReceiveNotification", oldUserNotification);
            await _hubContext.Clients.User(updated.UserId.ToString()).SendAsync("ReceiveNotification", newUserNotification);
        }
        else
        {
            // If userId has not changed, only send notification to the current user (homeowner)
            var notification = new Notification
            {
                UserId = currentUserId,
                TargetRole = "Homeowner",
                Type = "Vehicle",
                Title = "Vehicle Registration Updated",
                Message = $"Your vehicle registration information for vehicle {maskedPlateNumber} has been updated.",
                IsRead = false,
                DateCreated = DateTime.Now
            };

            _context.Notifications.Add(notification);

            // Save changes asynchronously
            await _context.SaveChangesAsync();

            // Send real-time notification to the current user (homeowner)
            await _hubContext.Clients.User(currentUserId.ToString()).SendAsync("ReceiveNotification", notification);
        }

        return Ok(vehicle);
    }

    // PUT: staff/VehicleRegistration/5/deactivate
    [HttpPut("vehicleregistration/{id}/deactivate")]
    public async Task<IActionResult> DeactivateVehicle(int id)
    {
        // Fetch the vehicle first to get the necessary details (e.g., UserId)
        var vehicle = _context.Vehicle_Registration.FirstOrDefault(v => v.VehicleId == id);
        if (vehicle == null)
        {
            return NotFound(new { success = false, message = "Vehicle not found." });
        }

        // Get the UserId of the owner of the vehicle
        int userId = vehicle.UserId;
        string plateNumber = vehicle.PlateNumber;

        // Mask the plate number (show only the last 4 characters)
        string maskedPlateNumber = MaskPlateNumber(plateNumber);

        // Set the vehicle status to Inactive instead of deleting
        vehicle.Status = "Inactive";
        _context.Vehicle_Registration.Update(vehicle);
        await _context.SaveChangesAsync();

        // Create a notification for the homeowner about the vehicle deactivation
        var notification = new Notification
        {
            UserId = userId,  // Send the notification to the vehicle owner
            TargetRole = "Homeowner",
            Type = "Vehicle",
            Title = "Vehicle Registration Deactivated",
            Message = $"Your vehicle Plate: {maskedPlateNumber} registration has been marked as inactive as of today {DateTime.Now:MM/dd/yyyy}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        // Add the notification to the context
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification to the user
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { success = true, message = "Vehicle marked as inactive successfully." });
    }

    //Activate vehicle registered
    [HttpPost("vehicle/activate/{id}")]
    public async Task<IActionResult> ActivateVehicle(int id)
    {
        var vehicle = await _context.Vehicle_Registration.FindAsync(id);
        if (vehicle == null)
            return NotFound();

        vehicle.Status = "Active";

        // Get the user id linked to this vehicle
        var userId = vehicle.UserId;

        var notification = new Notification
        {
            UserId = userId,
            TargetRole = "Homeowner",
            Type = "Vehicle",
            Title = "Vehicle Activated",
            Message = $"Your vehicle with plate number {vehicle.PlateNumber} has been activated as of {DateTime.Now:MM/dd/yyyy}.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok();
    }

    // Helper method to mask plate number
    private string MaskPlateNumber(string plateNumber)
    {
        if (string.IsNullOrEmpty(plateNumber))
        {
            return "Unknown Plate";
        }

        // Mask all characters except the last 4 characters
        int lengthToShow = 4;
        if (plateNumber.Length <= lengthToShow)
        {
            return plateNumber;  // No need to mask if the plate number is already short
        }

        string maskedPart = new string('*', plateNumber.Length - lengthToShow);
        string visiblePart = plateNumber.Substring(plateNumber.Length - lengthToShow);

        return maskedPart + visiblePart;
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
                            join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoGroup
                            from ui in userInfoGroup.DefaultIfEmpty()
                            where r.Status == status
                            select new ReservationViewModel
                            {
                                Id = r.ReservationId,
                                FacilityName = char.ToUpper(f.FacilityName[0]) + f.FacilityName.Substring(1),
                                RequestedBy = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                                SchedDate = r.SchedDate.ToString("MM/dd/yyyy"),
                                StartTime = r.StartTime, 
                                EndTime = r.EndTime,     
                                Status = r.Status
                            })
                            .OrderByDescending(r => r.Id)
                            .ToList();

        return Json(reservations);
    }

    public async Task<IActionResult> UpdateReservationStatus(int id, [FromBody] ReservationUpdateStatusDTO request)
    {
        // Fetch the reservation by ID
        var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.ReservationId == id);
        if (reservation == null)
        {
            return NotFound(new { message = "Reservation not found" });
        }

        if (request.Status != "Approved" && request.Status != "Declined")
        {
            return BadRequest(new { message = "Invalid status" });
        }

        // Fetch facility by FacilityId from the reservation
        var facility = await _context.Facility.FindAsync(reservation.FacilityId);
        if (facility == null)
        {
            return NotFound(new { message = "Facility not found for this reservation." });
        }

        // Store the original UserId to send notification
        var userId = reservation.UserId;

        // Update the reservation status
        reservation.Status = request.Status;
        await _context.SaveChangesAsync();

        string facilityName = facility.FacilityName;
        string schedDate = reservation.SchedDate.ToString("MM/dd/yyyy");

        // Set the message based on status
        string message = request.Status == "Approved"
            ? $"The Facility Reservation Request for {facilityName} has been approved for {schedDate} from {reservation.StartTime} to {reservation.EndTime}."
            : $"The Facility Reservation Request for {facilityName} has been declined.";

        // Create the notification
        var notification = new Notification
        {
            UserId = userId,
            TargetRole = "Homeowner",
            Type = "Facility Reservation Schedule",
            Title = $"Facility Reservation {request.Status}",
            Message = message,
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/facilities"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Real-time notification
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { message = $"The Facility Reservation has been {request.Status.ToLower()}." });
    }

    // ------------------------- For Facility Panel ------------------------- //
    //For fetching Facility
    [HttpGet("by-status/{status}")]
    public IActionResult GetFacilitiesByStatus(string status)
    {
        var facilities = _context.Facility
            .Where(f => f.Status == status)
            .Select(f => new {
                f.FacilityId,
                f.FacilityName,
                f.Description,
                f.Image,
                f.AvailableTime,
                f.Status
            }).ToList();

        return Ok(facilities);
    }

    [HttpGet("get-facility/{id}")]
    public IActionResult GetFacilityById(int id)
    {
        var facility = _context.Facility
            .Where(f => f.FacilityId == id)
            .Select(f => new
            {
                f.FacilityId,
                f.Image,
                f.FacilityName,
                f.Description,
                f.AvailableTime
            })
            .FirstOrDefault();

        if (facility == null)
            return NotFound();

        return Ok(facility);
    }

    [HttpPost("add-facility")]
    public async Task<IActionResult> AddFacility([FromForm] FacilityModficationDto model)
    {
        if (string.IsNullOrWhiteSpace(model.FacilityName))
            return BadRequest(new { message = "Facility name is required." });

        // Check if a facility with the same name already exists
        var existingFacility = await _context.Facility
            .Where(f => f.FacilityName.ToLower() == model.FacilityName.ToLower())
            .FirstOrDefaultAsync();

        if (existingFacility != null)
        {
            return BadRequest(new { message = "Facility name already exists. Please choose a different name." });
        }

        string imageFileName = null;

        if (model.Image != null && model.Image.Length > 0)
        {
            imageFileName = model.FacilityName.Replace(" ", "_") + ".jpg";
            var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/facilityimage", imageFileName);

            using (var stream = new FileStream(imagePath, FileMode.Create))
            {
                await model.Image.CopyToAsync(stream);
            }
        }

        var newFacility = new Facility
        {
            FacilityName = model.FacilityName,
            Description = model.Description,
            AvailableTime = model.AvailableTime,
            Image = imageFileName,
            Status = "Active"
        };

        _context.Facility.Add(newFacility);
        await _context.SaveChangesAsync();

        // Fetch all users
        var allUsers = await _context.User_Accounts.ToListAsync();

        bool staffNotified = false;
        bool adminNotified = false;

        var notificationsToAdd = new List<Notification>(); // Collect notifications here

        foreach (var user in allUsers)
        {
            if (user.Role == "Homeowner")
            {
                var homeownerNotification = new Notification
                {
                    Title = "Facility Created",
                    Message = $"Good news! The facility {model.FacilityName} has just been added. Take a moment to explore what's new.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Homeowner",
                    UserId = user.Id,
                    Link = "/home/facilities"
                };

                notificationsToAdd.Add(homeownerNotification);

                // Real-time notification to specific homeowner
                await _hubContext.Clients.User(user.Id.ToString())
                    .SendAsync("ReceiveNotification", homeownerNotification);
            }
            else if (user.Role == "Staff" && !staffNotified)
            {
                var staffNotification = new Notification
                {
                    Title = "Facility Created",
                    Message = $"A new facility named {model.FacilityName} has been added. You may now review and manage reservations accordingly.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Staff",
                    UserId = null,
                    Link = "/staff/requests/reservation"
                };

                notificationsToAdd.Add(staffNotification);

                await _hubContext.Clients.Group("staff")
                    .SendAsync("ReceiveNotification", staffNotification);

                staffNotified = true;
            }
            else if (user.Role == "Admin" && !adminNotified)
            {
                var adminNotification = new Notification
                {
                    Title = "Facility Created",
                    Message = $"A new facility named {model.FacilityName} has been added. Please verify the details.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Admin",
                    UserId = null,
                    Link = "/admin/facilities"
                };

                notificationsToAdd.Add(adminNotification);

                await _hubContext.Clients.Group("admin")
                    .SendAsync("ReceiveNotification", adminNotification);

                adminNotified = true;
            }
        }

        // Add all notifications at once and save
        await _context.Notifications.AddRangeAsync(notificationsToAdd);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("update-facility/{id}")]
    public async Task<IActionResult> UpdateFacility(int id, [FromForm] FacilityModficationDto model)
    {
        var facility = await _context.Facility.FindAsync(id);
        if (facility == null)
            return NotFound();

        var existingFacility = await _context.Facility
            .Where(f => f.FacilityName.ToLower() == model.FacilityName.ToLower() && f.FacilityId != id)
            .FirstOrDefaultAsync();

        if (existingFacility != null)
        {
            return BadRequest(new { message = "Facility name already exists. Please choose a different name." });
        }

        var oldFacilityName = facility.FacilityName;
        facility.FacilityName = model.FacilityName;
        facility.Description = model.Description;
        facility.AvailableTime = model.AvailableTime;

        var imageFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/facilityimage");
        var oldImageFileName = oldFacilityName.Replace(" ", "_") + ".jpg";
        var newImageFileName = model.FacilityName.Replace(" ", "_") + ".jpg";
        var oldImagePath = Path.Combine(imageFolderPath, oldImageFileName);
        var newImagePath = Path.Combine(imageFolderPath, newImageFileName);

        // Check if the facility name has changed and old image exists
        if (oldFacilityName != model.FacilityName && System.IO.File.Exists(oldImagePath))
        {
            System.IO.File.Move(oldImagePath, newImagePath);  // Rename image
            facility.Image = "/images/facilityimage/" + newImageFileName;
        }

        // If the model has a new image, process it
        if (model.Image != null && model.Image.Length > 0)
        {
            if (System.IO.File.Exists(newImagePath))
            {
                System.IO.File.Delete(newImagePath); // Delete existing image if new image is provided
            }

            using (var stream = new FileStream(newImagePath, FileMode.Create))
            {
                await model.Image.CopyToAsync(stream);
            }

            facility.Image = "/images/facilityimage/" + newImageFileName;
        }
        else if (string.IsNullOrEmpty(facility.Image))
        {
            facility.Image = "/images/facilityimage/" + newImageFileName;
        }
        await _context.SaveChangesAsync();

        // Fetch all users
        var allUsers = await _context.User_Accounts.ToListAsync();

        bool staffNotified = false;
        bool adminNotified = false;

        var notificationsToAdd = new List<Notification>(); // List to hold notifications

        foreach (var user in allUsers)
        {
            if (user.Role == "Homeowner")
            {
                var homeownerNotification = new Notification
                {
                    Title = "Facility Updated",
                    Message = $"A facility named {model.FacilityName} has been updated. You can check for changes.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Homeowner",
                    UserId = user.Id,
                    Link = "/home/facilities"
                };

                notificationsToAdd.Add(homeownerNotification);

                // Real-time notification to specific homeowner
                await _hubContext.Clients.User(user.Id.ToString())
                    .SendAsync("ReceiveNotification", homeownerNotification);
            }
            else if (user.Role == "Staff" && !staffNotified)
            {
                var staffNotification = new Notification
                {
                    Title = "Facility Updated",
                    Message = $"A facility named {model.FacilityName} has been updated. You can check for changes.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Staff",
                    UserId = null,
                    Link = "/staff/requests/reservation"
                };

                notificationsToAdd.Add(staffNotification);

                // Real-time notification to staff group
                await _hubContext.Clients.Group("staff")
                    .SendAsync("ReceiveNotification", staffNotification);

                staffNotified = true;
            }
            else if (user.Role == "Admin" && !adminNotified)
            {
                var adminNotification = new Notification
                {
                    Title = "Facility Updated",
                    Message = $"A facility named {model.FacilityName} has been updated. You can check for changes.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Admin",
                    UserId = null,
                    Link = "/admin/facilities"
                };

                notificationsToAdd.Add(adminNotification);

                // Real-time notification to admin group
                await _hubContext.Clients.Group("admin")
                    .SendAsync("ReceiveNotification", adminNotification);

                adminNotified = true;
            }
        }

        // Add all notifications to the database at once
        await _context.Notifications.AddRangeAsync(notificationsToAdd);

        // Save all changes at once
        await _context.SaveChangesAsync();
        return Ok();
    }

    public class FacilityModficationDto
    {
        public string FacilityName { get; set; }
        public string Description { get; set; }
        public string AvailableTime { get; set; }
        public IFormFile Image { get; set; }
    }

    [HttpPost("activate-facility/{id}")]
    public async Task<IActionResult> ActivateFacility(int id)
    {
        var facility = await _context.Facility.FindAsync(id);
        if (facility == null)
            return NotFound();

        // Change status to Active
        facility.Status = "Active";
        await _context.SaveChangesAsync();

        // Fetch all users
        var allUsers = await _context.User_Accounts.ToListAsync();

        // Get facility name
        var facilityName = facility.FacilityName;

        bool staffNotified = false;
        bool adminNotified = false;

        foreach (var user in allUsers)
        {
            if (user.Role == "Homeowner")
            {
                var homeownerNotification = new Notification
                {
                    Title = "Facility Activated",
                    Message = $"The facility named {facilityName} is now available for reservations. You can now make your reservation.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Homeowner",
                    UserId = user.Id,
                    Link = "/home/facilities"
                };

                _context.Notifications.Add(homeownerNotification);

                // Real-time notification to specific homeowner
                await _hubContext.Clients.User(user.Id.ToString())
                    .SendAsync("ReceiveNotification", homeownerNotification);
            }
            else if (user.Role == "Staff" && !staffNotified)
            {
                var staffNotification = new Notification
                {
                    Title = "Facility Activated",
                    Message = $"The facility named {facilityName} has been activated. You can review it.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Staff",
                    UserId = null,
                    Link = "/staff/requests/reservation"
                };

                _context.Notifications.Add(staffNotification);

                await _hubContext.Clients.Group("staff")
                    .SendAsync("ReceiveNotification", staffNotification);

                staffNotified = true;
            }
            else if (user.Role == "Admin" && !adminNotified)
            {
                var adminNotification = new Notification
                {
                    Title = "Facility Activated",
                    Message = $"The facility named {facilityName} has been added back. Please review if it is good.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Admin",
                    UserId = null,
                    Link = "/admin/facilities"
                };

                _context.Notifications.Add(adminNotification);

                await _hubContext.Clients.Group("admin")
                    .SendAsync("ReceiveNotification", adminNotification);

                adminNotified = true;
            }
        }

        return Ok();
    }

    [HttpPost("inactive-facility/{id}")]
    public async Task<IActionResult> DeleteFacility(int id)
    {
        var facility = await _context.Facility.FindAsync(id);
        if (facility == null)
            return NotFound();

        // Mark the facility as inactive
        facility.Status = "Inactive";
        await _context.SaveChangesAsync();

        var facilityName = facility.FacilityName;

        // Fetch all users
        var allUsers = await _context.User_Accounts.ToListAsync();

        bool staffNotified = false;
        bool adminNotified = false;

        foreach (var user in allUsers)
        {
            if (user.Role == "Homeowner")
            {
                var homeownerNotification = new Notification
                {
                    Title = "Facility Inactivated",
                    Message = $"The facility named '{facilityName}' has been removed. You can no longer make reservations at this time. We will notify you if it becomes available again. Thank you.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Homeowner",
                    UserId = user.Id,
                    Link = "/home/facilities"
                };

                _context.Notifications.Add(homeownerNotification);

                // Real-time notification to specific homeowner
                await _hubContext.Clients.User(user.Id.ToString())
                    .SendAsync("ReceiveNotification", homeownerNotification);
            }
            else if (user.Role == "Staff" && !staffNotified)
            {
                var staffNotification = new Notification
                {
                    Title = "Facility Inactivated",
                    Message = $"The facility named {facilityName} has been deactivated. Please check if there is some kind of error.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Staff",
                    UserId = null,
                    Link = "/staff/requests/reservation"
                };

                _context.Notifications.Add(staffNotification);

                await _hubContext.Clients.Group("staff")
                    .SendAsync("ReceiveNotification", staffNotification);

                staffNotified = true;
            }
            else if (user.Role == "Admin" && !adminNotified)
            {
                var adminNotification = new Notification
                {
                    Title = "Facility Inactivated",
                    Message = $"The facility named {facilityName} has been deactivated. Please check if there is some kind of mistake.",
                    Type = "Facility",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Admin",
                    UserId = null,
                    Link = "/admin/facilities"
                };

                _context.Notifications.Add(adminNotification);

                await _hubContext.Clients.Group("admin")
                    .SendAsync("ReceiveNotification", adminNotification);

                adminNotified = true;
            }
        }

        await _context.SaveChangesAsync();
        return Ok();
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
            var requests = await (
                from sr in _context.Service_Request
                where sr.Status == status
                join ua in _context.User_Accounts on sr.UserId equals ua.Id into userJoin
                from ua in userJoin.DefaultIfEmpty()
                join ui in _context.User_Info on ua.Id equals ui.UserAccountId into infoJoin
                from ui in infoJoin.DefaultIfEmpty()
                select new
                {
                    sr.ServiceRequestId,
                    sr.ReqType,
                    sr.Description,
                    sr.Status,
                    sr.DateSubmitted,
                    RejectedReason = sr.RejectedReason ?? string.Empty,
                    ScheduleDate = sr.ScheduleDate != null ? sr.ScheduleDate.Value.ToString("yyyy-MM-dd HH:mm") : null,
                    homeownerName = ui != null
                        ? char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " +
                          char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1)
                        : "No Name"
                }
            ).ToListAsync();

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

            // Get userId to notify
            var userId = serviceRequest.UserId;

            // Prepare notification message
            string message = $"The service request has been updated to {request.Status}.";
            string title = $"Service Request {request.Status}";

            if (request.Status == "Rejected" && !string.IsNullOrEmpty(request.RejectedReason))
            {
                serviceRequest.RejectedReason = request.RejectedReason;
                message = $"Your {serviceRequest.ReqType} service request has been rejected. Reason: {request.RejectedReason}";
            }
            else if (request.Status == "Cancelled")
            {
                message = $"Your {serviceRequest.ReqType} service request has been cancelled due to unforeseen circumstance. You can make a request again anytime.";
            }
            else if (request.Status == "Scheduled" && !string.IsNullOrEmpty(request.ScheduleDate))
            {
                // Parse the custom datetime format
                if (DateTime.TryParseExact(request.ScheduleDate, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    serviceRequest.ScheduleDate = parsedDate;
                    message = $"Your {serviceRequest.ReqType} service request has been scheduled for {parsedDate:MM/dd/yyyy hh:mm tt}.";
                }
                else
                {
                    return BadRequest(new { message = "Invalid date format. Use 'yyyy-MM-dd HH:mm:ss'." });
                }
            }
            else if (request.Status == "Ongoing")
            {
                message = $"Your {serviceRequest.ReqType} service request is Ongoing as of {DateTime.Now.ToString("MM/dd/yyyy")}."; 
            }
            else if (request.Status == "Completed")
            {
                message = $"Your {serviceRequest.ReqType} service request is Completed as of {DateTime.Now.ToString("MM/dd/yyyy")}. " +
                          "Please check if it is all good now, if there is still problem you can make a request anytime. Have a great day!";
            }

            await _context.SaveChangesAsync();

            // Create notification
            var notification = new Notification
            {
                UserId = userId,
                TargetRole = "Homeowner",
                Type = "Service Schedule",
                Title = title,
                Message = message,
                IsRead = false,
                DateCreated = DateTime.Now,
                Link = "/home/services"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send real-time notification
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

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
                      join ui in _context.User_Info on user.Id equals ui.UserAccountId into userInfoGroup
                      from ui in userInfoGroup.DefaultIfEmpty()
                      select new
                      {
                          bill.BillId,
                          bill.BillName,
                          bill.DueDate,
                          bill.Status,
                          BillAmount = bill.BillAmount,
                          FullName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " +
                                     char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1)
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
        var homeowners = (from u in _context.User_Accounts
                        where u.Role == "Homeowner" && u.Status == "ACTIVE"
                        join ui in _context.User_Info on u.Id equals ui.UserAccountId into infoJoin
                        from ui in infoJoin.DefaultIfEmpty()
                        select new
                        {
                            userId = u.Id,
                            fullName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " +
                                       char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                            email = u.Email
                        }
                        ).ToListAsync();
            return Ok(homeowners);
    }

    [HttpGet("bills/getbyid/{id}")]
    public IActionResult GetBillById(int id)
    {
        var bill = _context.Bill.FirstOrDefault(b => b.BillId == id);
        if (bill == null) return NotFound();
        return Ok(bill);
    }

    // Adding new Bill data
    [HttpPost("bills/add")]
    public async Task<IActionResult> AddBill([FromBody] Bill model)
    {
        // Get the bill status based on the due date
        model.Status = GetBillStatus(model.DueDate);

        // Add the bill to the context
        _context.Bill.Add(model);

        int userId = model.UserId;

        // Create a new notification
        var notification = new Notification
        {
            UserId = userId,
            TargetRole = "Homeowner",
            Type = "Bill",
            Title = "New Bill Posted",
            Message = $"💰 A new bill '{model.BillName}' due on {model.DueDate} has been added. Please check or you can call us anytime if there is any question or concern.",
            IsRead = false, 
            DateCreated = DateTime.Now,
            Link = $"/home/bill"
        };

        // Add the notification to the context
        _context.Notifications.Add(notification);

        // Save changes asynchronously
        await _context.SaveChangesAsync();

        // Send real-time notification to the user
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { message = "Bill added successfully!" });
    }

    [HttpPut("bills/update")]
    public async Task<IActionResult> UpdateBill([FromBody] Bill updated)
    {
        var bill = _context.Bill.FirstOrDefault(b => b.BillId == updated.BillId);
        if (bill == null) return NotFound();

        bill.BillName = updated.BillName;
        bill.DueDate = updated.DueDate;
        bill.BillAmount = updated.BillAmount;
        bill.Status = GetBillStatus(updated.DueDate);
        bill.UserId = updated.UserId;

        // Create a new notification for the bill update
        var notification = new Notification
        {
            UserId = updated.UserId,
            TargetRole = "Homeowner",
            Type = "Bill",
            Title = "Bill Updated",
            Message = $"✏️ The bill '{updated.BillName}' has been updated. Due date: {updated.DueDate}. Please check or you can call us anytime if there is any question or concern.",
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/bill"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send SignalR notification to user
        await _hubContext.Clients.User(updated.UserId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { message = "Bill updated successfully!" });
    }


    [HttpDelete("bills/delete/{id}")]
    public async Task<IActionResult> DeleteBill(int id)
    {
        var bill = _context.Bill.FirstOrDefault(b => b.BillId == id);
        if (bill == null) return NotFound();

        bill.Status = "Deleted";

        // Create a new notification for the deletion
        var notification = new Notification
        {
            UserId = bill.UserId,
            TargetRole = "Homeowner",
            Type = "Bill",
            Title = "Bill Deleted",
            Message = $"🗑️ The bill '{bill.BillName}' scheduled for {bill.DueDate} has been deleted. Please call us if you are already paid.",
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/bill"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification to user
        await _hubContext.Clients.User(bill.UserId.ToString()).SendAsync("ReceiveNotification", notification);

        return Ok(new { message = "Bill deleted successfully!" });
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
                            join ui in _context.User_Info on user.Id equals ui.UserAccountId into userInfoGroup
                            from ui in userInfoGroup.DefaultIfEmpty()
                            where _context.Payment.Any(p => p.BillId == bill.BillId)
                            select new
                            {
                                bill.BillId,
                                bill.BillName,
                                bill.DueDate,
                                bill.Status,
                                bill.BillAmount,
                                FullName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1)
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
            .Select(p => new Payments
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
        var now = DateTime.Now.Date;

        var allPolls = await _context.Poll.ToListAsync();
        var pollsWithStatusChanged = new List<Poll>();

        foreach (var poll in allPolls)
        {
            if (DateTime.TryParse(poll.StartDate, out DateTime startDate) &&
                DateTime.TryParse(poll.EndDate, out DateTime endDate))
            {
                if ((now < startDate.Date || now > endDate.Date) && poll.Status == true)
                {
                    poll.Status = false;
                    pollsWithStatusChanged.Add(poll);
                }
            }
        }

        if (pollsWithStatusChanged.Any())
        {
            var titlesList = pollsWithStatusChanged.Select(p => $"• {p.Title}").ToList();
            var formattedTitles = string.Join("\n", titlesList);
            string message = $"You can check and review it. The following poll(s) date is done:\n\n{formattedTitles}";

            var staffNotification = new Notification
            {
                Title = "Poll Ended",
                Message = message,
                Type = "Poll",
                IsRead = false,
                DateCreated = DateTime.Now,
                TargetRole = "Staff",
                UserId = null,
                Link = "/staff/polls"
            };
            _context.Notifications.Add(staffNotification);

            var adminNotification = new Notification
            {
                Title = "Poll Ended",
                Message = message,
                Type = "Poll",
                IsRead = false,
                DateCreated = DateTime.Now,
                TargetRole = "Admin",
                UserId = null
            };
            _context.Notifications.Add(adminNotification);

            var homeowners = await _context.User_Accounts
                .Where(u => u.Role == "Homeowner")
                .ToListAsync();

            foreach (var homeowner in homeowners)
            {
                var homeownerNotification = new Notification
                {
                    Title = "Poll Ended",
                    Message = $"You may no longer submit your vote. The following poll(s) date is done:\n\n{formattedTitles}",
                    IsRead = false,
                    DateCreated = DateTime.Now,
                    TargetRole = "Homeowner",
                    UserId = homeowner.Id,
                    Type = "Poll"
                };

                _context.Notifications.Add(homeownerNotification);

                await _hubContext.Clients.User(homeowner.Id.ToString())
                    .SendAsync("ReceiveNotification", homeownerNotification);
            }

            await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", staffNotification);
            await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", adminNotification);
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

        // Create Staff notification
        var staffNotification = new Notification
        {
            Title = "New Poll Created",
            Message = $"The Poll Name {request.Title} created. Please check to review it.",
            Type = "Poll",
            IsRead = false,
            DateCreated = DateTime.Now,
            TargetRole = "Staff",
            UserId = null,
            Link = "/staff/poll_management"
        };
        _context.Notifications.Add(staffNotification);

        // Create Admin notification
        var adminNotification = new Notification
        {
            Title = "New Poll Created",
            Message = $"The Poll Name {request.Title} created. You can check and review choices if it is good for voting and contact Staff if there is any changes.",
            Type = "Poll",
            IsRead = false,
            DateCreated = DateTime.Now,
            TargetRole = "Admin",
            UserId = null,
            Link = "/admin/poll"
        };
        _context.Notifications.Add(adminNotification);

        // Get all Homeowners and send them individual notifications
        var homeowners = await _context.User_Accounts
            .Where(u => u.Role == "Homeowner")
            .ToListAsync();

        foreach (var homeowner in homeowners)
        {
            var homeownerNotification = new Notification
            {
                Title = "New Poll Created",
                Message = $"The Poll Name {request.Title} created. You can check it to review it.",
                IsRead = false,
                Type = "Poll",
                DateCreated = DateTime.Now,
                TargetRole = "Homeowner",
                UserId = homeowner.Id,
                Link = "/home/dashboard"
            };

            _context.Notifications.Add(homeownerNotification);

            // Send real-time notification to each Homeowner
            await _hubContext.Clients.User(homeowner.Id.ToString()).SendAsync("ReceiveNotification", homeownerNotification);
        }

        await _context.SaveChangesAsync();

        // Send real-time notifications to Staff and Admin
        await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", staffNotification);
        await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", adminNotification);

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

        poll.Title = request.Title;
        poll.Description = request.Description;
        poll.StartDate = request.StartDate;
        poll.EndDate = request.EndDate;

        var existingChoicesDetails = await _context.Poll_Choice
            .Where(c => c.PollId == pollId)
            .ToListAsync();

        var existingChoiceMap = existingChoicesDetails.ToDictionary(
            c => c.Choice.Trim().ToLower(),
            c => c
        );

        var updatedChoiceIds = new HashSet<int>();

        foreach (var (newRaw, index) in newChoicesRaw.Select((val, i) => (val, i)))
        {
            var normalized = newRaw.ToLower();

            if (existingChoiceMap.TryGetValue(normalized, out var existing))
            {
                updatedChoiceIds.Add(existing.ChoiceId);
                continue;
            }

            if (index < existingChoicesDetails.Count)
            {
                var toRename = existingChoicesDetails[index];
                toRename.Choice = newRaw;
                updatedChoiceIds.Add(toRename.ChoiceId);
            }
            else
            {
                _context.Poll_Choice.Add(new Poll_Choice
                {
                    PollId = pollId,
                    Choice = newRaw
                });
            }
        }

        foreach (var existing in existingChoicesDetails)
        {
            if (!updatedChoiceIds.Contains(existing.ChoiceId))
            {
                bool hasVotes = await _context.Vote.AnyAsync(v => v.ChoiceId == existing.ChoiceId);
                if (!hasVotes)
                {
                    _context.Poll_Choice.Remove(existing);
                }
            }
        }

        await _context.SaveChangesAsync();

        // === Notifications ===

        // Staff notification
        var staffNotification = new Notification
        {
            Title = "Poll Updated",
            Message = $"The Poll Name {request.Title} has been updated. You can review for changes.",
            Type = "Poll",
            IsRead = false,
            DateCreated = DateTime.Now,
            TargetRole = "Staff",
            UserId = null,
            Link = "/staff/poll_management"
        };
        _context.Notifications.Add(staffNotification);

        // Admin notification
        var adminNotification = new Notification
        {
            Title = "Poll Updated",
            Message = $"The Poll Name {request.Title} has been updated. You can check and review choices if it is good for voting and contact Staff if there is any changes.",
            Type = "Poll",
            IsRead = false,
            DateCreated = DateTime.Now,
            TargetRole = "Admin",
            UserId = null,
            Link = "/admin/poll"
        };
        _context.Notifications.Add(adminNotification);

        // Homeowner notifications
        var homeowners = await _context.User_Accounts
            .Where(u => u.Role == "Homeowner")
            .ToListAsync();

        foreach (var homeowner in homeowners)
        {
            var homeownerNotification = new Notification
            {
                Title = "Poll Updated",
                Message = $"The Poll Name {request.Title} has been updated. You can check for changes.",
                IsRead = false,
                DateCreated = DateTime.Now,
                TargetRole = "Homeowner",
                UserId = homeowner.Id,
                Type = "Poll",
                Link = "/home/dashboard"
            };

            _context.Notifications.Add(homeownerNotification);

            await _hubContext.Clients.User(homeowner.Id.ToString())
                .SendAsync("ReceiveNotification", homeownerNotification);
        }

        await _context.SaveChangesAsync();

        // Send real-time notifications to Staff and Admin
        await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", staffNotification);
        await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", adminNotification);

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

    [HttpPatch("polls/{pollId}")]
    public async Task<IActionResult> SoftDeletePoll(int pollId)
    {
        var poll = await _context.Poll.FindAsync(pollId);
        if (poll == null) return NotFound();

        poll.Status = false;  // Mark as Inactive
        await _context.SaveChangesAsync();

        string message = $"The poll titled {poll.Title} has been deactivated. You cannot vote anymore.";

        // === Create and send Staff notification ===
        var staffNotification = new Notification
        {
            Title = "Poll Deactivated",
            Message = message,
            IsRead = false,
            DateCreated = DateTime.Now,
            TargetRole = "Staff",
            Type = "Poll",
            UserId = null,
            Link = "/staff/polls"
        };
        _context.Notifications.Add(staffNotification);

        // === Create and send Admin notification ===
        var adminNotification = new Notification
        {
            Title = "Poll Deactivated",
            Message = message,
            Type = "Poll",
            IsRead = false,
            DateCreated = DateTime.Now,
            TargetRole = "Admin",
            UserId = null
        };
        _context.Notifications.Add(adminNotification);

        // === Send to all Homeowners ===
        var homeowners = await _context.User_Accounts
            .Where(u => u.Role == "Homeowner")
            .ToListAsync();

        foreach (var homeowner in homeowners)
        {
            var homeownerNotification = new Notification
            {
                Title = "Poll Deactivated",
                Message = message,
                IsRead = false,
                DateCreated = DateTime.Now,
                TargetRole = "Homeowner",
                UserId = homeowner.Id,
                Type = "Poll"
            };

            _context.Notifications.Add(homeownerNotification);

            // Real-time push to individual homeowner
            await _hubContext.Clients.User(homeowner.Id.ToString())
                .SendAsync("ReceiveNotification", homeownerNotification);
        }

        await _context.SaveChangesAsync();

        // === Real-time push to Staff and Admin groups ===
        await _hubContext.Clients.Group("staff")
            .SendAsync("ReceiveNotification", staffNotification);
        await _hubContext.Clients.Group("admin")
            .SendAsync("ReceiveNotification", adminNotification);

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

    [Route("event_management")]
    public IActionResult Event()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [Route("feedbacks")]
    public IActionResult Feedback()
    {
        RefreshJwtCookies();
        var role = GetUserRoleFromToken();
        if (string.IsNullOrEmpty(role) || role != "Staff")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [HttpGet("getfeedbacklist")]
    public IActionResult GetFeedbackList(string type)
    {
        var list = (
            from f in _context.Feedback
            where f.FeedbackType == type && (type != "Complaint" || f.ComplaintStatus != "Resolved")
            join u in _context.User_Accounts on f.UserId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            join ui in _context.User_Info on u.Id equals ui.UserAccountId into infoJoin
            from ui in infoJoin.DefaultIfEmpty()
            orderby f.DateSubmitted descending
            select new
            {
                f.FeedbackId,
                f.FeedbackType,
                f.Description,
                f.ComplaintStatus,
                f.DateSubmitted,
                f.UserId,
                FullName = (u != null)
                    ? (ui.Firstname ?? "").Substring(0, 1).ToUpper() + (ui.Firstname ?? "").Substring(1).ToLower() + " " +
                      (ui.Lastname ?? "").Substring(0, 1).ToUpper() + (ui.Lastname ?? "").Substring(1).ToLower()
                    : "No User"
            }
        ).ToList();

        return Json(list);
    }

    [HttpGet("getresolvedfeedback")]
    public IActionResult GetResolvedFeedback()
    {
        var feedbacks = (from f in _context.Feedback
                         join u in _context.User_Accounts on f.UserId equals u.Id
                         join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoGroup
                         from ui in userInfoGroup.DefaultIfEmpty()
                         where f.FeedbackType == "Complaint" && f.ComplaintStatus == "Resolved"
                         orderby f.DateSubmitted descending
                         select new
                         {
                             f.FeedbackId,
                             f.FeedbackType,
                             f.Description,
                             f.ComplaintStatus,
                             f.DateSubmitted,
                             FullName = (ui.Firstname ?? "").Substring(0, 1).ToUpper() + (ui.Firstname ?? "").Substring(1).ToLower() + " " +
                                        (ui.Lastname ?? "").Substring(0, 1).ToUpper() + (ui.Lastname ?? "").Substring(1).ToLower()
                         }).ToList();

        return Ok(feedbacks);
    }

    [HttpGet("getfeedbackdetails")]
    public IActionResult GetFeedbackDetails(int feedbackId)
    {
        var feedback = (from f in _context.Feedback
                        join u in _context.User_Accounts on f.UserId equals u.Id
                        join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoGroup
                        from ui in userInfoGroup.DefaultIfEmpty()
                        where f.FeedbackId == feedbackId 
                        select new
                        {
                            f.FeedbackId,
                            f.FeedbackType,
                            f.Description,
                            f.DateSubmitted,
                            f.ComplaintStatus,
                            Rating = f.Rating,
                            FullName =
                                (char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1).ToLower()) + " " +
                                (char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1).ToLower())
                        }).FirstOrDefault();

        if (feedback == null)
        {
            return NotFound(new { message = "Feedback not found" });
        }

        return Ok(feedback);
    }

    [HttpGet("getconversation")]
    public IActionResult GetConversation(int feedbackId)
    {
        var convo = (
            from c in _context.FeedbackConversation
            where c.FeedbackId == feedbackId
            join u in _context.User_Accounts on c.UserId equals u.Id into userJoin
            from u in userJoin.DefaultIfEmpty()
            join ui in _context.User_Info on u.Id equals ui.UserAccountId into infoJoin
            from ui in infoJoin.DefaultIfEmpty()
            orderby c.DateSent
            select new
            {
                c.ConvoId,
                c.FeedbackId,
                c.SenderRole,
                c.Message,
                c.DateSent,
                c.UserId,
                FullName = ui != null
                    ? (ui.Firstname ?? "").Substring(0, 1).ToUpper() + (ui.Firstname ?? "").Substring(1).ToLower() + " " +
                      (ui.Lastname ?? "").Substring(0, 1).ToUpper() + (ui.Lastname ?? "").Substring(1).ToLower()
                    : "No User",
                ProfileImage = ui != null ? ui.Profile : null
            }
        ).ToList();

        return Json(convo);
    }

    [HttpPost("sendmessage")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageDTO dto)
    {
        var Iduser = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(Iduser, out int userId))
        {
            return Unauthorized("User not authenticated.");
        }

        var convo = new Feedback_Conversation
        {
            FeedbackId = dto.FeedbackId,
            SenderRole = "Staff",
            Message = dto.Message,
            DateSent = DateTime.Now,
            UserId = userId 
        };
        _context.FeedbackConversation.Add(convo);
        _context.SaveChanges();

        // Create a notification for the homeowner
        var notification = new Notification
        {
            UserId = dto.RecepientUserId, 
            TargetRole = "Homeowner",
            Type = "Feedback Message",
            Title = "Complaint Feedback Message",
            Message = $"You have a new message on your complaint feedback.",
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/feedbacks"
        };

        // Add the notification to the context
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification to the user
        await _hubContext.Clients.User(dto.RecepientUserId.ToString()).SendAsync("ReceiveNotification", notification);
        return Ok();
    }

    [HttpPost("markongoing")]
    public async Task<IActionResult> MarkOngoing(int feedbackId)
    {
        var feedback = _context.Feedback.FirstOrDefault(f => f.FeedbackId == feedbackId && f.ComplaintStatus == "Pending");

        if (feedback == null)
        {
            return BadRequest(new { error = "Invalid feedback or already updated." });
        }

        // Update complaint status
        feedback.ComplaintStatus = "Ongoing";
        _context.SaveChanges();

        // Create a notification for the homeowner
        var notification = new Notification
        {
            UserId = feedback.UserId,
            TargetRole = "Homeowner",
            Type = "Feedback Message",
            Title = "Complaint Feedback Status",
            Message = "The complaint you submitted is currently being reviewed.",
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/feedbacks"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients.User(feedback.UserId.ToString())
            .SendAsync("ReceiveNotification", notification);

        return Ok(new { success = true });
    }

    [HttpPost("markresolved/{id}")]
    public async Task<IActionResult> MarkResolved(int id)
    {
        var feedback = _context.Feedback.FirstOrDefault(f => f.FeedbackId == id);
        if (feedback == null)
        {
            return NotFound("Feedback not found.");
        }

        // Update complaint status
        feedback.ComplaintStatus = "Resolved";
        _context.SaveChanges();

        // Create a notification for the homeowner using the UserId from feedback
        var notification = new Notification
        {
            UserId = feedback.UserId, // Use UserId from the feedback record
            TargetRole = "Homeowner",
            Type = "Feedback Message",
            Title = "Complaint Feedback Status",
            Message = "The complaint you submitted is resolved.",
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/feedbacks"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification
        await _hubContext.Clients.User(feedback.UserId.ToString())
            .SendAsync("ReceiveNotification", notification);

        return Ok();
    }

    public class SendMessageDTO
    {
        public int FeedbackId { get; set; }
        public int RecepientUserId { get; set; }
        public string Message { get; set; }
    }

    //For Setting a schedule
    [HttpPost("scheduleservice")]
    public async Task<IActionResult> ScheduleService([FromBody] ScheduleServiceDTO dto)
    {
        if (dto.ScheduleDate < DateTime.Now)
        {
            return BadRequest(new { success = false, message = "Schedule date must be in the future." });
        }

        try
        {
            var request = new Service_Request
            {
                ReqType = dto.ReqType,
                Description = dto.Description,
                Status = "Scheduled",
                DateSubmitted = DateTime.Now.ToString("yyyy-MM-dd"),
                UserId = dto.HomeownerId,
                ScheduleDate = dto.ScheduleDate
            };

            _context.Service_Request.Add(request);

            var notification = new Notification
            {
                UserId = dto.HomeownerId,
                TargetRole = "Homeowner",
                Type = "Service Schedule",
                Title = "Service Scheduled",
                Message = $"You have a scheduled service. Please expect our worker on {dto.ScheduleDate:MM/dd/yyyy hh:mm tt} to come to your home and make sure anyone you trusted is at home for the visitation of our service worker. Thank you and have a great day ahead!",
                IsRead = false,
                DateCreated = DateTime.Now,
                Link = "/home/feedbacks"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(dto.HomeownerId.ToString()).SendAsync("ReceiveNotification", notification);

            return Ok(new { success = true, message = "Service successfully scheduled." });
        }
        catch (Exception ex)
        {
            // Log error if necessary
            return StatusCode(500, new { success = false, message = "Something went wrong while scheduling the service." });
        }
    }

    public class ScheduleServiceDTO
    {
        public int HomeownerId { get; set; }
        public string ReqType { get; set; }
        public string Description { get; set; }
        public DateTime ScheduleDate { get; set; }
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
            .AsEnumerable() 
            .Where(r => r.Status == "Approved") 
            .Select(r => new
            {
                Month = DateTime.Parse(r.SchedDate.ToString()).ToString("MM/dd/yyyy")
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
                Month = DateTime.ParseExact(p.DatePaid, "yyyy-MM-dd", CultureInfo.InvariantCulture).ToString("MM/dd/yyyy"),
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
            .Where(f => f.FeedbackType == "Compliment") // Filter by Type = 'Complement'
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

    //Fetch Data Choices for vehicle type and brand
    [HttpGet("getvehiclefilteroptions")]
    public IActionResult GetVehicleFilterOptions()
    {
        var types = _context.Vehicle_Registration
            .Select(v => v.Type)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var vehiclebrands = _context.Vehicle_Registration
            .Select(v => v.VehicleBrand)
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        return Json(new { types, vehiclebrands });
    }


    //For fetching data for report
    [HttpPost("GetReportData")]
    public IActionResult GetReportData(string reportType, string status, string startDate, string endDate, string vehicleType, string vehiclebrand)
    {
        var result = new List<object>();

        switch (reportType)
        {
            case "VEHICLE_REGISTRATION":
                var vehicleQuery = (
                    from vehicle in _context.Vehicle_Registration
                    join user in _context.User_Accounts
                        on vehicle.UserId equals user.Id into userJoin
                    from user in userJoin.DefaultIfEmpty()
                    join info in _context.User_Info
                        on user.Id equals info.UserAccountId into infoJoin
                    from info in infoJoin.DefaultIfEmpty()
                    select new { vehicle, user, info }
                ).AsQueryable();

                if (!string.IsNullOrEmpty(vehicleType) && vehicleType != "All")
                {
                    vehicleQuery = vehicleQuery.Where(v => v.vehicle.Type == vehicleType);
                }

                if (!string.IsNullOrEmpty(vehiclebrand) && vehiclebrand != "All")
                {
                    vehicleQuery = vehicleQuery.Where(v => v.vehicle.VehicleBrand == vehiclebrand);
                }

                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    vehicleQuery = vehicleQuery.Where(v => v.vehicle.Status == status);
                }

                result = vehicleQuery.Select(v => new
                {
                    v.vehicle.VehicleId,
                    v.vehicle.PlateNumber,
                    v.vehicle.VehicleBrand,
                    v.vehicle.Type,
                    v.vehicle.Color,
                    v.vehicle.Status,
                    OwnerName = char.ToUpper(v.info.Firstname[0]) + v.info.Firstname.Substring(1).ToLower() + " " +
                                char.ToUpper(v.info.Lastname[0]) + v.info.Lastname.Substring(1).ToLower()
                }).ToList<object>();
                break;

            case "RESERVATIONS":
                DateTime.TryParse(startDate, out var startR);
                DateTime.TryParse(endDate, out var endR);

                var startDateOnly = DateOnly.FromDateTime(startR);
                var endDateOnly = DateOnly.FromDateTime(endR);

                var reservationsQuery = (
                    from res in _context.Reservations
                    join fac in _context.Facility
                        on res.FacilityId equals fac.FacilityId
                    join user in _context.User_Accounts
                        on res.UserId equals user.Id into userJoin
                    from user in userJoin.DefaultIfEmpty()
                    join info in _context.User_Info
                        on user.Id equals info.UserAccountId into infoJoin
                    from info in infoJoin.DefaultIfEmpty()
                    where res.SchedDate >= startDateOnly && res.SchedDate <= endDateOnly
                    select new { res, fac, user, info }
                ).AsQueryable();

                if (status != "All")
                {
                    reservationsQuery = reservationsQuery.Where(x => x.res.Status == status);
                }
                else
                {
                    reservationsQuery = reservationsQuery.Where(x =>
                        x.res.Status == "Approved" ||
                        x.res.Status == "Declined");
                }

                var reservations = reservationsQuery
                    .Select(x => new
                    {
                        x.res.ReservationId,
                        FacilityName = x.fac.FacilityName,
                        DateReserved = x.res.SchedDate.ToString("MM/dd/yyyy"),
                        x.res.StartTime,
                        x.res.EndTime,
                        x.res.Status,
                        ReservedBy = char.ToUpper(x.info.Firstname[0]) + x.info.Firstname.Substring(1).ToLower() + " " +
                                     char.ToUpper(x.info.Lastname[0]) + x.info.Lastname.Substring(1).ToLower()
                    }).ToList<object>();

                result = reservations;
                break;

            case "SERVICE_REQUEST":
                DateTime.TryParse(startDate, out var startS);
                DateTime.TryParse(endDate, out var endS);
                string startDateStr = startS.ToString("yyyy-MM-dd HH:mm:ss");
                string endDateStr = endS.ToString("yyyy-MM-dd HH:mm:ss");

                var serviceQuery = (
                    from request in _context.Service_Request
                    join user in _context.User_Accounts
                        on request.UserId equals user.Id into userJoin
                    from user in userJoin.DefaultIfEmpty()
                    join info in _context.User_Info
                        on user.Id equals info.UserAccountId into infoJoin
                    from info in infoJoin.DefaultIfEmpty()
                    where string.Compare(request.DateSubmitted, startDateStr) >= 0 &&
                          string.Compare(request.DateSubmitted, endDateStr) <= 0
                    select new { request, user, info }
                ).AsQueryable();

                // Filter by status
                if (status != "All")
                {
                    serviceQuery = serviceQuery.Where(x => x.request.Status == status);
                }
                else
                {
                    serviceQuery = serviceQuery.Where(x =>
                        x.request.Status == "Scheduled" ||
                        x.request.Status == "Completed" ||
                        x.request.Status == "Cancelled" ||
                        x.request.Status == "Rejected");
                }

                var services = serviceQuery
                    .AsEnumerable()
                    .Select(x =>
                    {
                        var obj = new ExpandoObject() as IDictionary<string, object>;

                        obj["ServiceRequestId"] = x.request.ServiceRequestId;
                        obj["ReqType"] = x.request.ReqType;
                        obj["Description"] = x.request.Description;
                        obj["Status"] = x.request.Status;

                        if (DateTime.TryParse(x.request.DateSubmitted, out var submittedDate))
                            obj["DateSubmitted"] = submittedDate.ToString("MM/dd/yyyy hh:mm tt").ToUpper();

                        if (x.request.Status == "Rejected" && !string.IsNullOrEmpty(x.request.RejectedReason))
                            obj["RejectedReason"] = x.request.RejectedReason;

                        if ((x.request.Status == "Scheduled" || x.request.Status == "Completed" || x.request.Status == "Cancelled")
                            && x.request.ScheduleDate.HasValue)
                        {
                            obj["ScheduleDate"] = x.request.ScheduleDate.Value.ToString("MM/dd/yyyy hh:mm tt").ToUpper();
                        }

                        obj["RequestedBy"] =
                            char.ToUpper(x.info.Firstname[0]) + x.info.Firstname.Substring(1).ToLower() + " " +
                            char.ToUpper(x.info.Lastname[0]) + x.info.Lastname.Substring(1).ToLower();

                        return obj;
                    })
                    .ToList<object>();

                result = services;
                break;

            case "VISITOR_PASSES":
                DateTime.TryParse(startDate, out var startV);
                DateTime.TryParse(endDate, out var endV);

                var visitorQuery = from pass in _context.Visitor_Pass
                                   join user in _context.User_Accounts on pass.UserId equals user.Id
                                   join info in _context.User_Info on user.Id equals info.UserAccountId into infoJoin
                                   from info in infoJoin.DefaultIfEmpty()
                                   where pass.DateTime >= startV && pass.DateTime <= endV
                                   select new
                                   {
                                       pass.VisitorId,
                                       pass.VisitorName,
                                       DateTime = pass.DateTime.ToString("MM/dd/yyyy hh:mm tt").ToUpper(),
                                       pass.Status,
                                       pass.Relationship,
                                       HomeownerName = Capitalize(info.Firstname) + " " + Capitalize(info.Lastname)
                                   };

                if (status != "All")
                {
                    visitorQuery = visitorQuery.Where(v => v.Status == status);
                }

                var passes = visitorQuery.ToList<object>();
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
        var user = await (
            from ua in _context.User_Accounts
            join ui in _context.User_Info
                on ua.Id equals ui.UserAccountId into uiJoin
            from ui in uiJoin.DefaultIfEmpty()
            where ua.Id == userId
            select new
            {
                Profile = !string.IsNullOrEmpty(ui.Profile) ? ui.Profile : (ui.Profile ?? ""),
                Firstname = ui.Firstname,
                Lastname = ui.Lastname,
                Email = ua.Email,
                Contact = ui.PhoneNumber,
                Address = ui.Address
            }
        ).FirstOrDefaultAsync();

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

        var user = await _context.User_Info
            .FirstOrDefaultAsync(ui => ui.UserAccountId == userId);

        if (user == null) return NotFound();

        if (user == null) return NotFound();

        var ext = Path.GetExtension(file.FileName);
        var name = $"{char.ToUpper(user.Firstname[0])}{user.Lastname}-{user.UserAccountId}{ext}";

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

        var user = await _context.User_Accounts
            .Where(u => u.Id == userId)
            .Include(u => u.User_Info) // Assumes navigation property
            .FirstOrDefaultAsync();

        if (user == null || user.User_Info == null)
            return NotFound();

        // Full name check
        var nameExists = await _context.User_Info
            .Where(ui => ui.Firstname.ToLower() == request.Firstname.ToLower() &&
                         ui.Lastname.ToLower() == request.Lastname.ToLower() &&
                         ui.UserAccountId != userId)
            .AnyAsync();

        if (nameExists)
            return Conflict(new { message = "Another user already has the same full name." });

        // Contact check
        var contactExists = await _context.User_Info
            .Where(ui => ui.PhoneNumber == request.Contact && ui.UserAccountId != userId)
            .AnyAsync();

        if (contactExists)
            return Conflict(new { message = "Contact already in use." });

        // ✅ Update actual entities
        user.Email = request.Email;
        user.User_Info.Firstname = request.Firstname;
        user.User_Info.Lastname = request.Lastname;
        user.User_Info.PhoneNumber = request.Contact;
        user.User_Info.Address = request.Address;

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

    [Route("notifications")]
    public IActionResult Notifications()
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
