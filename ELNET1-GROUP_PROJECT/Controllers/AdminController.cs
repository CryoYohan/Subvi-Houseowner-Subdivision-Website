using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Mail;
using System.Data;
using System.Linq;
using System.Net.Mime;
using System.Text;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ELNET1_GROUP_PROJECT.SignalR;
using Microsoft.AspNetCore.SignalR;
using DocumentFormat.OpenXml.Spreadsheet;
using System.Dynamic;

public class AdminController : Controller
{
    private readonly MyAppDBContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public AdminController(MyAppDBContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        ViewData["Layout"] = "_AdminLayout";
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
    public IActionResult RedirectToDashboard()
    {
        return RedirectToAction("Dashboard");
    }

    [Route("admin/communityforum")]
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
            return RedirectToAction("CommunityForum");
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

    [HttpGet]
    public IActionResult Comments(int id, string title)
    {
        RefreshJwtCookies();
        // Fetch the post and replies based on the PostId (id)
        var post = (
            from f in _context.Forum
            join u in _context.User_Accounts on f.UserId equals u.Id
            join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoJoin
            from ui in userInfoJoin.DefaultIfEmpty()
            where f.PostId == id
            select new
            {
                f.PostId,
                Title = !string.IsNullOrEmpty(f.Title) ? char.ToUpper(f.Title[0]) + f.Title.Substring(1) : "",
                Hashtag = f.Hashtag,
                f.Content,
                f.DatePosted,
                f.UserId,
                u.Role,
                ui.Profile,
                Firstname = ui != null && !string.IsNullOrEmpty(ui.Firstname) ? char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) : "",
                Lastname = ui != null && !string.IsNullOrEmpty(ui.Lastname) ? char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1) : ""
            }
        ).FirstOrDefaultAsync();

        // Fetch replies and join with User_Accounts to get the FullName
        var replies = (from r in _context.Replies
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
                Role = u.Role,
                Profile = ui.Profile,
                FullName = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                Firstname = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1),
                Lastname = char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1)
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

    public IActionResult Facilities()
    {
        RefreshJwtCookies();
        return View();
    }

    //For fetching Facility
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

    public async Task<IActionResult> InactiveFacility(int id)
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
                    Message = $"The facility named {facilityName} has been removed. You can no longer make reservations at this time. We will notify you if it becomes available again. Thank you.",
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

    [Route("event_schedules")]
    public IActionResult Event()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    public IActionResult Dashboard()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        var announcements = _context.Announcement
        .OrderByDescending(a => a.DatePosted)
        .ToList();

        return View(announcements);
    }

    public IActionResult Reservations()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    [HttpGet("/admin/reservations/schedule")]
    public async Task<IActionResult> GetAdminReservations([FromQuery] string status = "Approved")
    {
        var query = from r in _context.Reservations
                    join f in _context.Facility on r.FacilityId equals f.FacilityId
                    join u in _context.User_Accounts on r.UserId equals u.Id
                    join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoJoin
                    from ui in userInfoJoin.DefaultIfEmpty()
                    where r.Status == "Approved" || r.Status == "Declined"
                    select new ReservationViewModel
                    {
                        Id = r.ReservationId,
                        FacilityName = char.ToUpper(f.FacilityName[0]) + f.FacilityName.Substring(1),
                        RequestedBy = char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1) + " " + char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1),
                        SchedDate = r.SchedDate.ToString("MM/dd/yyyy"),
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                        Status = r.Status
                    };

        if (status != "All")
        {
            query = query.Where(r => r.Status == status);
        }

        var reservations = await query.ToListAsync();
        return Json(reservations);
    }

    public IActionResult HomeownerStaffAccounts()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }

        // Perform LEFT JOIN to combine USER_ACCOUNT with USER_INFO
        var users = (from ua in _context.User_Accounts
                     join ui in _context.User_Info on ua.Id equals ui.UserAccountId into joined
                     from ui in joined.DefaultIfEmpty()
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

    [HttpPost]
    // POST: /Admin/AddUserAccount
    public async Task<IActionResult> AddUserAccount(UserDataRequest model)
    {
        if (ModelState.IsValid)
        {
            RefreshJwtCookies();
            try
            {
                // Check if the email already exists in USER_ACCOUNT
                var existingEmail = _context.User_Accounts.FirstOrDefault(u => u.Email == model.Email);
                if (existingEmail != null)
                {
                    TempData["ErrorMessage"] = "User already exists with this email.";
                    return RedirectToAction("HomeownerStaffAccounts");
                }

                // Check if the contact number already exists in USER_INFO
                var existingContact = _context.User_Info.FirstOrDefault(ui => ui.PhoneNumber == model.PhoneNumber);
                if (existingContact != null)
                {
                    TempData["ErrorMessage"] = "User already exists with this contact number.";
                    return RedirectToAction("HomeownerStaffAccounts");
                }

                // Check if first and last name combination already exists
                var existingName = _context.User_Info.FirstOrDefault(ui =>
                    ui.Firstname.ToLower() == model.Firstname.ToLower() &&
                    ui.Lastname.ToLower() == model.Lastname.ToLower());

                if (existingName != null)
                {
                    TempData["ErrorMessage"] = "User with the same name already exists.";
                    return RedirectToAction("HomeownerStaffAccounts");
                }

                // Save original password for email and notification
                string originalPassword = model.Password;
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(originalPassword);

                // Create and save User Account (credentials)
                var userAccount = new User_Account
                {
                    Email = model.Email,
                    Password = hashedPassword,
                    Role = model.Role,
                    Status = "ACTIVE",
                    DateRegistered = DateTime.Now
                };

                _context.User_Accounts.Add(userAccount);
                _context.SaveChanges(); // Save first to get the USER_ID (auto-incremented)

                // Create and save User Info (profile data)
                var userInfo = new User_Info
                {
                    Firstname = model.Firstname,
                    Lastname = model.Lastname,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    DateCreated = DateTime.Now,
                    UserAccountId = userAccount.Id // foreign key reference
                };

                _context.User_Info.Add(userInfo);
                _context.SaveChanges();

                // Send email if Homeowner
                string fullname = $"{model.Firstname} {model.Lastname}";
                if (model.Role == "Homeowner")
                {
                    SendEmail(model.Email, fullname, model.Email, originalPassword);
                }

                // Notification Link
                string link = model.Role switch
                {
                    "Homeowner" => "/home/settings",
                    "Admin" => "/admin/settings",
                    "Staff" => "/staff/profile/settings",
                    _ => "/"
                };

                var notification = new Notification
                {
                    Title = "Account Created",
                    Message = $"Your user account was successfully created. Email: {model.Email}, Password: {originalPassword}. Please change your password for security reasons.",
                    IsRead = false,
                    Type = "Account",
                    TargetRole = model.Role,
                    Link = link,
                    UserId = model.Role == "Homeowner" ? userAccount.Id : null,
                    DateCreated = DateTime.Now
                };

                _context.Notifications.Add(notification);
                _context.SaveChanges();

                // Send real-time notification via SignalR
                if (model.Role == "Homeowner")
                {
                    await _hubContext.Clients.User(userAccount.Id.ToString()).SendAsync("ReceiveNotification", new
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                    });
                }
                else if (model.Role == "Staff")
                {
                    await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                    });
                }
                else if (model.Role == "Admin")
                {
                    await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
                    {
                        Title = notification.Title,
                        Message = notification.Message,
                        DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                    });
                }

                TempData["SuccessMessage"] = "User registered successfully!";
                return RedirectToAction("HomeownerStaffAccounts");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to register new account. Try again later.";
                return RedirectToAction("HomeownerStaffAccounts");
            }
        }

        return View(model);
    }

    // Email sending function
    private bool SendEmail(string recipientEmail, string fullname, string username, string password)
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

            // Local path to logo
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
                    .credentials {{ background: #f8fafc; border-radius: 8px; padding: 1.5rem; margin: 1rem 0; }}
                    .footer {{ padding: 1.5rem; text-align: center; color: #718096; font-size: 0.9rem; }}
                    .button {{ background: #4299e1; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; display: inline-block; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <img src='cid:company-logo' alt='Subvi Logo' style='height: 60px; margin-bottom: 1rem;'>
                        <h1 style='color: white; margin: 0;'>Welcome to Subvi House Subdivision</h1>
                    </div>
                
                    <div class='content'>
                        <h2 style='color: #2d3748; margin-top: 0;'>Hello, {fullname}!</h2>
                        <p style='color: #4a5568; line-height: 1.6;'>
                            Your account has been successfully created. Here are your login credentials:
                        </p>

                        <div class='credentials'>
                            <p style='margin: 0.5rem 0;'>
                                <strong style='color: #2d3748;'>Username:</strong> 
                                <span style='color: #4a5568;'>{username}</span>
                            </p>
                            <p style='margin: 0.5rem 0;'>
                                <strong style='color: #2d3748;'>Password:</strong> 
                                <span style='color: #4a5568;'>{password}</span>
                            </p>
                        </div>

                        <p style='color: #4a5568; line-height: 1.6;'>
                            For security reasons, we recommend changing your password after your first login.
                        </p>

                        <div style='text-align: center; margin: 2rem 0;'>
                            <a href='{websiteUrl}/login' class='button' 
                               style='background: linear-gradient(135deg, #4299e1, #3182ce); 
                                      transition: transform 0.2s ease;
                                      box-shadow: 0 4px 6px rgba(66, 153, 225, 0.2);'>
                                Login Now
                            </a>
                        </div>
                    </div>

                    <div class='footer'>
                        <p style='margin: 0.5rem 0;'>
                            Best regards,<br>
                            <strong>Subvi Management Team</strong>
                        </p>
                        <p style='margin: 1rem 0; font-size: 0.8rem;'>
                            This is an automated message - please do not reply directly to this email
                        </p>
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
                Subject = "Welcome to Subvi House Subdivision - Your Account Details",
                Body = emailBody,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(recipientEmail);

            // Fixed logo embedding
            var logo = new LinkedResource(logoPath)
            {
                ContentId = "company-logo", 
                ContentType = new ContentType("image/png") 
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
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }
        return false;
    }

    /*
    // Function to delete all users
    public IActionResult DeleteAllUsers()
    {
        RefreshJwtCookies();
        var allUsers = _context.User_Accounts.ToList(); // Retrieve all users

        _context.User_Accounts.RemoveRange(allUsers); // Remove all users
        _context.SaveChanges(); // Save changes to the database

        // Optionally, you can add a success message here or redirect
        return RedirectToAction("HomeownerStaffAccounts"); // Or display a confirmation
    }
    */

    [HttpPost]
    // POST: /Admin/EditUser
    public async Task<IActionResult> EditUser(UserDataRequest model)
    {
        RefreshJwtCookies();
        ModelState.Remove("Password");

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
            return RedirectToAction("HomeownerStaffAccounts");
        }

        try
        {
            var existingUser = _context.User_Accounts.FirstOrDefault(u => u.Id == model.Id);
            var userInfo = _context.User_Info.FirstOrDefault(u => u.UserAccountId == model.Id);

            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Check for duplicate email (excluding current user)
            if (existingUser.Email != model.Email &&
                _context.User_Accounts.Any(u => u.Email == model.Email && u.Id != model.Id))
            {
                TempData["ErrorMessage"] = "Email is already taken by another user.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Check for duplicate contact (in USER_INFO)
            if (!string.IsNullOrEmpty(model.PhoneNumber) &&
                _context.User_Info.Any(u => u.PhoneNumber == model.PhoneNumber && u.UserAccountId != model.Id))
            {
                TempData["ErrorMessage"] = "Contact number is already used by another user.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Check for duplicate name (in USER_INFO)
            if (_context.User_Info.Any(u => u.Firstname == model.Firstname && u.Lastname == model.Lastname && u.UserAccountId != model.Id))
            {
                TempData["ErrorMessage"] = "A user with the same name already exists.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Update USER_ACCOUNT
            existingUser.Email = model.Email;
            existingUser.Role = model.Role;

            if (!string.IsNullOrEmpty(model.Password))
            {
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            // Update USER_INFO
            if (userInfo != null)
            {
                userInfo.Firstname = model.Firstname;
                userInfo.Lastname = model.Lastname;
                userInfo.Address = model.Address;
                userInfo.PhoneNumber = model.PhoneNumber;
            }
            else
            {
                // If no USER_INFO exists, create one
                userInfo = new User_Info
                {
                    UserAccountId = model.Id,
                    Firstname = model.Firstname,
                    Lastname = model.Lastname,
                    Address = model.Address,
                    PhoneNumber = model.PhoneNumber,
                    DateCreated = DateTime.Now
                };
                _context.User_Info.Add(userInfo);
            }

            await _context.SaveChangesAsync();

            // Role-based link assignment
            string link = model.Role switch
            {
                "Homeowner" => "/home/settings",
                "Admin" => "/admin/settings",
                "Staff" => "/staff/profile/settings",
                _ => "/"
            };

            string Capitalize(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return input;
                return char.ToUpper(input[0]) + input.Substring(1).ToLower();
            }

            var fullName = $"{Capitalize(model.Firstname)} {Capitalize(model.Lastname)}";

            var notification = new Notification
            {
                Title = "Account Updated",
                Message = $"Hi {fullName}, your account has been updated by the administrator. Go to your profile settings for the changes. You can contact us anytime if there is something wrong.",
                IsRead = false,
                Type = "Account Update",
                TargetRole = model.Role,
                UserId = model.Role == "Homeowner" ? model.Id : null,
                Link = link,
                DateCreated = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var signalPayload = new
            {
                Title = notification.Title,
                Message = notification.Message,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            };

            if (model.Role == "Homeowner")
            {
                await _hubContext.Clients.User(model.Id.ToString()).SendAsync("ReceiveNotification", signalPayload);
            }
            else if (model.Role == "Staff")
            {
                await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", signalPayload);
            }
            else if (model.Role == "Admin")
            {
                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", signalPayload);
            }

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction("HomeownerStaffAccounts");
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update user info. Please try again later.";
            return RedirectToAction("HomeownerStaffAccounts");
        }
    }

    // Function to deactivate a specific user by ID
    public async Task<IActionResult> DeleteUser(int id)
    {
        RefreshJwtCookies();

        try
        {
            // Find the user by ID, including related user info
            var user = await _context.User_Accounts
                .Include(u => u.User_Info)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Mark the user as INACTIVE instead of deleting
            user.Status = "INACTIVE";
            await _context.SaveChangesAsync();

            // Use names from User_Info if available
            string Capitalize(string input)
            {
                return string.IsNullOrWhiteSpace(input) ? input
                    : char.ToUpper(input[0]) + input.Substring(1).ToLower();
            }

            string firstName = user.User_Info?.Firstname ?? user.User_Info.Firstname ?? "User";
            string lastName = user.User_Info?.Lastname ?? user.User_Info.Lastname ?? "";

            var fullName = $"{Capitalize(firstName)} {Capitalize(lastName)}";

            var notification = new Notification
            {
                Title = "Account Deactivated",
                Message = $"Hi {fullName}, your account has been set to inactive by the administrator. Contact support if this was unexpected.",
                IsRead = false,
                Type = "Account Deactivation",
                TargetRole = user.Role,
                UserId = user.Role == "Homeowner" ? user.Id : null,
                DateCreated = DateTime.Now
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            // SignalR notification payload
            var signalPayload = new
            {
                Title = notification.Title,
                Message = notification.Message,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            };

            if (user.Role == "Homeowner")
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", signalPayload);
            }
            else if (user.Role == "Staff")
            {
                await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", signalPayload);
            }
            else if (user.Role == "Admin")
            {
                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", signalPayload);
            }

            TempData["SuccessMessage"] = "User deactivated successfully!";
            return RedirectToAction("HomeownerStaffAccounts");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to deactivate user. Please try again later.";
            return RedirectToAction("HomeownerStaffAccounts");
        }
    }

    [HttpPost]
    public async Task<IActionResult> ActivateUser(int id)
    {
        RefreshJwtCookies();
        try
        {
            var user = await _context.User_Accounts
                .Include(u => u.User_Info)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            user.Status = "ACTIVE";
            _context.Update(user);
            await _context.SaveChangesAsync();

            // Capitalize full name
            string Capitalize(string input)
            {
                if (string.IsNullOrWhiteSpace(input)) return input;
                return char.ToUpper(input[0]) + input.Substring(1).ToLower();
            }
            var fullName = $"{Capitalize(user.User_Info.Firstname)} {Capitalize(user.User_Info.Lastname)}";

            // Create Notification
            var notification = new Notification
            {
                Title = "Account Activated",
                Message = $"Hi {fullName}, your account has been reactivated. You can now access the system again.",
                IsRead = false,
                Type = "Account Activation",
                TargetRole = user.Role,
                UserId = user.Role == "Homeowner" ? user.Id : null,
                DateCreated = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // SignalR payload
            var signalPayload = new
            {
                Title = notification.Title,
                Message = notification.Message,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            };

            if (user.Role == "Homeowner")
            {
                await _hubContext.Clients.User(user.Id.ToString()).SendAsync("ReceiveNotification", signalPayload);
            }
            else if (user.Role == "Staff")
            {
                await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", signalPayload);
            }
            else if (user.Role == "Admin")
            {
                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", signalPayload);
            }

            TempData["SuccessMessage"] = "User activated and notified successfully!";
            return RedirectToAction("HomeownerStaffAccounts");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "An error occurred while activating the user.";
            return RedirectToAction("HomeownerStaffAccounts");
        }
    }

    public IActionResult PaymentHistory()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    public async Task<IActionResult> GetBills(string status = "Paid")
    {
        var billsWithUser = from bill in _context.Bill
                            join user in _context.User_Accounts
                                on bill.UserId equals user.Id
                            join ui in _context.User_Info on user.Id equals ui.UserAccountId into userInfoJoin
                            from ui in userInfoJoin.DefaultIfEmpty()
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

    [HttpGet]
    public async Task<IActionResult> GetBillPayments(int billId)
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

    public IActionResult Services()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    // Get service requests by status
    public async Task<IActionResult> GetServiceRequests(string status)
    {
        try
        {
            var requests = await _context.Service_Request
                .Where(r => r.Status == status)
                .Join(
                    _context.User_Accounts,
                    sr => sr.UserId,
                    ua => ua.Id,
                    (sr, ua) => new { sr, ua }
                )
                .Join(
                    _context.User_Info,
                    temp => temp.ua.Id,
                    ui => ui.UserAccountId,
                    (temp, ui) => new
                    {
                        temp.sr.ServiceRequestId,
                        temp.sr.ReqType,
                        temp.sr.Description,
                        temp.sr.Status,
                        temp.sr.DateSubmitted,
                        RejectedReason = temp.sr.RejectedReason ?? string.Empty,
                        ScheduleDate = temp.sr.ScheduleDate != null ?
                            temp.sr.ScheduleDate.Value.ToString("yyyy-MM-dd HH:mm") : null,
                        HomeownerName =
                            (ui.Firstname != null && ui.Lastname != null)
                                ? char.ToUpper(ui.Firstname[0]) + ui.Firstname.Substring(1).ToLower() + " " +
                                  char.ToUpper(ui.Lastname[0]) + ui.Lastname.Substring(1).ToLower()
                                : "No Name"
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

    public IActionResult Announcements()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        var announcements = _context.Announcement
            .OrderByDescending(a => a.DatePosted)
            .ToList();
        return View(announcements);
    }

    // Add a new announcement
    [HttpPost]
    public async Task<IActionResult> AddAnnouncement(string title, string description)
    {
        RefreshJwtCookies();
        var userIdStr = Request.Cookies["Id"];

        if (!int.TryParse(userIdStr, out int userId))
        {
            TempData["ErrorMessage"] = "Invalid User ID.";
            return RedirectToAction("Announcements");
        }

        try
        {
            var newAnnouncement = new Announcement
            {
                Title = title,
                Description = description,
                DatePosted = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                UserId = userId
            };

            _context.Announcement.Add(newAnnouncement);
            await _context.SaveChangesAsync();

            // Email Notification Logic 
            //For all user who are active
            var users = _context.User_Accounts
                .Where(u => u.Status == "ACTIVE")
                .Include(u => u.User_Info)
                .ToList();

            /*
            // For testing target specific id
            var users = _context.User_Accounts
                .Where(u => new[] { 17, 18 }.Contains(u.Id))
                .ToList();
            */

            int emailSuccess = 0;
            int emailFail = 0;

            foreach (var user in users)
            {
                string Capitalize(string s) => string.IsNullOrWhiteSpace(s) ? "" : char.ToUpper(s[0]) + s.Substring(1).ToLower();
                var fullName = $"{Capitalize(user.User_Info.Firstname)} {Capitalize(user.User_Info.Lastname)}";

                if (!string.IsNullOrEmpty(user.Email))
                {
                    var success = SendEmailAnnouncement(user.Email, fullName, title, description);
                    if (success) emailSuccess++;
                    else emailFail++;
                }
                else
                {
                    emailFail++;
                }
            }

            // In-App Notification Logic
            var notifTitle = "New Announcement Posted";
            var notifMessage = $"A new announcement titled {title} was posted. Please check.";
            var link = "/home/dashboard";

            // Homeowner notifications
            var homeowners = users.Where(u => u.Role == "Homeowner").ToList();

            foreach (var h in homeowners)
            {
                var notif = new Notification
                {
                    Title = notifTitle,
                    Message = notifMessage,
                    IsRead = false,
                    Type = "Announcement",
                    TargetRole = "Homeowner",
                    Link = link,
                    UserId = h.Id,
                    DateCreated = DateTime.Now
                };
                _context.Notifications.Add(notif);

                await _hubContext.Clients.User(h.Id.ToString()).SendAsync("ReceiveNotification", new
                {
                    Title = notif.Title,
                    Message = notif.Message,
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });
            }

            // Staff notification (broadcast)
            var staffNotif = new Notification
            {
                Title = notifTitle,
                Message = notifMessage,
                IsRead = false,
                Type = "Announcement",
                TargetRole = "Staff",
                Link = "/staff/dashboard",
                UserId = null,
                DateCreated = DateTime.Now
            };
            _context.Notifications.Add(staffNotif);

            await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
            {
                Title = staffNotif.Title,
                Message = staffNotif.Message,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            });

            // Admin notification (broadcast)
            var adminNotif = new Notification
            {
                Title = notifTitle,
                Message = notifMessage,
                IsRead = false,
                Type = "Announcement",
                TargetRole = "Admin",
                Link = "/admin/announcements",
                UserId = null,
                DateCreated = DateTime.Now
            };
            _context.Notifications.Add(adminNotif);

            await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
            {
                Title = adminNotif.Title,
                Message = adminNotif.Message,
                DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
            });

            await _context.SaveChangesAsync();

            // TempData message based on email result
            if (users.Count == 0)
            {
                TempData["SuccessMessage"] = "Announcement added successfully, but no users were notified.";
            }
            else if (emailSuccess == 0)
            {
                TempData["SuccessMessage"] = "Announcement added, but all email notifications failed.";
            }
            else if (emailFail > 0)
            {
                TempData["SuccessMessage"] = $"Announcement added and email sent to {emailSuccess}, but {emailFail} failed.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Announcement added and email sent successfully to all users ({emailSuccess}).";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.InnerException?.Message ?? ex.Message;
        }

        return RedirectToAction("Announcements");
    }

    private bool SendEmailAnnouncement(string recipientEmail, string fullname, string title, string description)
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
                    .announcement-box {{ background: #ebf8ff; border-left: 4px solid #4299e1; padding: 1.5rem; border-radius: 6px; }}
                    .footer {{ padding: 1.5rem; text-align: center; color: #718096; font-size: 0.9rem; }}
                    .button {{ background: #4299e1; color: white; padding: 12px 24px; border-radius: 6px; text-decoration: none; display: inline-block; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <img src='cid:company-logo' alt='Subvi Logo' style='height: 60px; margin-bottom: 1rem;'>
                        <h1 style='color: white; margin: 0;'>New Announcement</h1>
                    </div>

                    <div class='content'>
                        <h2 style='color: #2d3748;'>Hello {fullname},</h2>
                        <p style='color: #4a5568; line-height: 1.6;'>We have a new announcement for you. Please see the details below:</p>

                        <div class='announcement-box'>
                            <h3 style='margin-top: 0; color: #2b6cb0;'>{title}</h3>
                            <p style='color: #2d3748;'>{description}</p>
                        </div>

                        <div style='text-align: center; margin-top: 2rem;'>
                            <a href='{websiteUrl}/home/dashboard' class='button'
                               style='background: linear-gradient(135deg, #4299e1, #3182ce);
                                      transition: transform 0.2s ease;
                                      box-shadow: 0 4px 6px rgba(66, 153, 225, 0.2);'>
                                View More Announcements
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
                Subject = $"📢 New Announcement: {title}",
                Body = emailBody,
                IsBodyHtml = true,
            };

            mailMessage.To.Add(recipientEmail);

            // Embed the logo
            var logo = new LinkedResource(logoPath)
            {
                ContentId = "company-logo",
                ContentType = new ContentType("image/png")
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
            Console.WriteLine($"Failed to send announcement email: {ex.Message}");
        }

        return false;
    }

    [HttpPost]
    public async Task<IActionResult> EditAnnouncement(int id, string title, string description)
    {
        RefreshJwtCookies();

        try
        {
            var announcement = _context.Announcement.Find(id);
            if (announcement != null)
            {
                announcement.Title = title;
                announcement.Description = description;
                _context.SaveChanges();

                var notifTitle = "Announcement Updated";
                var notifMessage = $"The announcement {title} has been updated.";
                var link = "/home/dashboard";

                // Notify Homeowners
                var homeowners = _context.User_Accounts
                    .Where(u => u.Role == "Homeowner")
                    .Select(u => u.Id)
                    .ToList();

                foreach (var idHomeowner in homeowners)
                {
                    var notif = new Notification
                    {
                        Title = notifTitle,
                        Message = notifMessage,
                        IsRead = false,
                        Type = "Announcement",
                        TargetRole = "Homeowner",
                        Link = link,
                        UserId = idHomeowner,
                        DateCreated = DateTime.Now
                    };
                    _context.Notifications.Add(notif);

                    await _hubContext.Clients.User(idHomeowner.ToString()).SendAsync("ReceiveNotification", new
                    {
                        Title = notif.Title,
                        Message = notif.Message,
                        DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                    });
                }

                // Staff notification
                var staffNotif = new Notification
                {
                    Title = notifTitle,
                    Message = notifMessage,
                    IsRead = false,
                    Type = "Announcement",
                    TargetRole = "Staff",
                    Link = "/staff/dashboard",
                    DateCreated = DateTime.Now
                };
                _context.Notifications.Add(staffNotif);

                await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
                {
                    Title = staffNotif.Title,
                    Message = staffNotif.Message,
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });

                // Admin notification
                var adminNotif = new Notification
                {
                    Title = notifTitle,
                    Message = notifMessage,
                    IsRead = false,
                    Type = "Announcement",
                    TargetRole = "Admin",
                    Link = "/admin/announcements",
                    DateCreated = DateTime.Now
                };
                _context.Notifications.Add(adminNotif);

                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
                {
                    Title = adminNotif.Title,
                    Message = adminNotif.Message,
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });

                _context.SaveChanges();

                TempData["SuccessMessage"] = "Announcement updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Announcement not found.";
            }
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update announcement. Please try again.";
        }

        return RedirectToAction("Announcements");
    }

    // Delete an announcement
    [HttpPost]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        RefreshJwtCookies();

        try
        {
            var announcement = _context.Announcement.Find(id);
            if (announcement != null)
            {
                var title = announcement.Title;
                _context.Announcement.Remove(announcement);
                _context.SaveChanges();

                var notifTitle = "Announcement Deleted";
                var notifMessage = $"The announcement {title} has been deleted.";

                // Notify Homeowners
                var homeowners = _context.User_Accounts
                    .Where(u => u.Role == "Homeowner")
                    .Select(u => u.Id)
                    .ToList();

                foreach (var idHomeowner in homeowners)
                {
                    var notif = new Notification
                    {
                        Title = notifTitle,
                        Message = notifMessage,
                        IsRead = false,
                        Type = "Announcement",
                        TargetRole = "Homeowner",
                        Link = "/home/dashboard",
                        UserId = idHomeowner,
                        DateCreated = DateTime.Now
                    };
                    _context.Notifications.Add(notif);

                    await _hubContext.Clients.User(idHomeowner.ToString()).SendAsync("ReceiveNotification", new
                    {
                        Title = notif.Title,
                        Message = notif.Message,
                        DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                    });
                }

                // Staff notification
                var staffNotif = new Notification
                {
                    Title = notifTitle,
                    Message = notifMessage,
                    IsRead = false,
                    Type = "Announcement",
                    TargetRole = "Staff",
                    Link = "/staff/dashboard",
                    DateCreated = DateTime.Now
                };
                _context.Notifications.Add(staffNotif);

                await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", new
                {
                    Title = staffNotif.Title,
                    Message = staffNotif.Message,
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });

                // Admin notification
                var adminNotif = new Notification
                {
                    Title = notifTitle,
                    Message = notifMessage,
                    IsRead = false,
                    Type = "Announcement",
                    TargetRole = "Admin",
                    Link = "/admin/announcements",
                    DateCreated = DateTime.Now
                };
                _context.Notifications.Add(adminNotif);

                await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", new
                {
                    Title = adminNotif.Title,
                    Message = adminNotif.Message,
                    DateCreated = DateTime.Now.ToString("MM/dd/yyyy")
                });

                _context.SaveChanges();
                TempData["SuccessMessage"] = "Announcement deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Announcement not found.";
            }
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to delete announcement. Please try again.";
        }

        return RedirectToAction("Announcements");
    }

    public IActionResult Poll()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }

    public async Task<IActionResult> GetPolls([FromQuery] string status = "active")
    {
        bool isActive = status.ToLower() != "inactive";
        var polls = await _context.Poll
            .Where(p => p.Status == isActive)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync();

        return Ok(polls);
    }

    public async Task<IActionResult> GetPoll(int pollId)
    {
        var poll = await _context.Poll.FindAsync(pollId);
        if (poll == null) return NotFound();
        return Ok(poll);
    }

    public async Task<IActionResult> GetChoices(int pollId)
    {
        var choices = await _context.Poll_Choice
            .Where(c => c.PollId == pollId)
            .ToListAsync();

        return Ok(choices);
    }

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

    public IActionResult Feedback()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Admin");
        }
        return View();
    }

    public IActionResult GetFeedbackList(string type)
    {
        var list = _context.Feedback
            .Where(f => f.FeedbackType == type && (type != "Complaint" || f.ComplaintStatus != "Resolved"))
            .Join(_context.User_Accounts,
                  f => f.UserId,
                  ua => ua.Id,
                  (f, ua) => new { f, ua })
            .Join(_context.User_Info,
                  temp => temp.ua.Id,
                  ui => ui.UserAccountId,
                  (temp, ui) => new
                  {
                      temp.f.FeedbackId,
                      temp.f.FeedbackType,
                      temp.f.Description,
                      temp.f.ComplaintStatus,
                      temp.f.DateSubmitted,
                      FullName = (ui.Firstname ?? "").Substring(0, 1).ToUpper() + (ui.Firstname ?? "").Substring(1).ToLower() + " " +
                                 (ui.Lastname ?? "").Substring(0, 1).ToUpper() + (ui.Lastname ?? "").Substring(1).ToLower()
                  })
            .OrderByDescending(f => f.DateSubmitted)
            .ToList();

        return Json(list);
    }

    public IActionResult GetResolvedFeedback()
    {
        var feedbacks = (from f in _context.Feedback
                         join u in _context.User_Accounts on f.UserId equals u.Id
                         join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoJoin
                         from ui in userInfoJoin.DefaultIfEmpty()
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

    public IActionResult GetFeedbackDetails(int feedbackId)
    {
        var feedback = (from f in _context.Feedback
                        join u in _context.User_Accounts on f.UserId equals u.Id
                        join ui in _context.User_Info on u.Id equals ui.UserAccountId into userInfoJoin
                        from ui in userInfoJoin.DefaultIfEmpty()
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

    public IActionResult Reports()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
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
            .Where(f => f.FeedbackType == "Compliment") 
            .GroupBy(f => f.Rating) 
            .Select(g => new { Rating = g.Key, Count = g.Count() }) 
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

    //For fetching choices data for vehicle report
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

    [HttpPost]
    public IActionResult GetReportData(string reportType, string status, string startDate, string endDate, string vehicleType, string vehiclebrand, string role)
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
                    .Join(_context.User_Info,
                        combined => combined.user.Id,
                        info => info.UserAccountId,
                        (combined, info) => new
                        {
                            combined.vehicle.VehicleId,
                            combined.vehicle.PlateNumber,
                            combined.vehicle.Type,
                            combined.vehicle.Color,
                            combined.vehicle.VehicleBrand,
                            combined.vehicle.Status,
                            FullName = (info.Firstname ?? "").Substring(0, 1).ToUpper() + (info.Firstname ?? "").Substring(1).ToLower() + " " +
                                       (info.Lastname ?? "").Substring(0, 1).ToUpper() + (info.Lastname ?? "").Substring(1).ToLower(),
                        });

                if (!string.IsNullOrEmpty(vehicleType) && vehicleType != "All")
                {
                    vehicleQuery = vehicleQuery.Where(v => v.Type == vehicleType);
                }

                if (!string.IsNullOrEmpty(vehiclebrand) && vehiclebrand != "All")
                {
                    vehicleQuery = vehicleQuery.Where(v => v.VehicleBrand == vehiclebrand);
                }

                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    vehicleQuery = vehicleQuery.Where(v => v.Status == status);
                }

                result = vehicleQuery.Select(v => new
                {
                    v.VehicleId,
                    v.PlateNumber,
                    v.VehicleBrand,
                    v.Type,
                    v.Color,
                    v.Status,
                    OwnerName = v.FullName
                }).ToList<object>();
                break;

            case "RESERVATIONS":
                DateTime.TryParse(startDate, out var startR);
                DateTime.TryParse(endDate, out var endR);

                var startDateOnly = DateOnly.FromDateTime(startR);
                var endDateOnly = DateOnly.FromDateTime(endR);

                var reservationsQuery = _context.Reservations
                    .Join(_context.Facility,
                        res => res.FacilityId,
                        fac => fac.FacilityId,
                        (res, fac) => new { res, fac })
                    .Join(_context.User_Accounts,
                        combined => combined.res.UserId,
                        user => user.Id,
                        (combined, user) => new { combined.res, combined.fac, user })
                    .Join(_context.User_Info,
                        combined => combined.user.Id,
                        info => info.UserAccountId,
                        (combined, info) => new { combined.res, combined.fac, user = combined.user, info })
                    .Where(x =>
                        x.res.SchedDate >= startDateOnly &&
                        x.res.SchedDate <= endDateOnly);

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

                var serviceQuery = _context.Service_Request
                    .Join(_context.User_Accounts,
                        request => request.UserId,
                        user => user.Id,
                        (request, user) => new { request, user })
                    .Join(_context.User_Info,
                        combined => combined.user.Id,
                        userInfo => userInfo.UserAccountId,
                        (combined, userInfo) => new { combined.request, combined.user, userInfo })
                    .Where(x =>
                        string.Compare(x.request.DateSubmitted, startDateStr) >= 0 &&
                        string.Compare(x.request.DateSubmitted, endDateStr) <= 0);

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
                            char.ToUpper(x.userInfo.Firstname[0]) + x.userInfo.Firstname.Substring(1).ToLower() + " " +
                            char.ToUpper(x.userInfo.Lastname[0]) + x.userInfo.Lastname.Substring(1).ToLower();

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
                                   join info in _context.User_Info on user.Id equals info.UserAccountId
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

            case "USER_ACCOUNT":
                var userQuery = from user in _context.User_Accounts
                                join info in _context.User_Info on user.Id equals info.UserAccountId
                                select new { user, info };

                if (!string.IsNullOrEmpty(startDate) && !string.IsNullOrEmpty(endDate))
                {
                    DateTime.TryParse(startDate, out startV);
                    DateTime.TryParse(endDate, out endV);
                    userQuery = userQuery.Where(u => u.user.DateRegistered >= startV && u.user.DateRegistered <= endV);
                }

                if (!string.IsNullOrEmpty(status) && status != "All")
                {
                    userQuery = userQuery.Where(u => u.user.Status == status.ToUpper());
                }

                if (!string.IsNullOrEmpty(role) && role != "All")
                {
                    userQuery = userQuery.Where(u => u.user.Role == role);
                }

                // Arranged By Role
                userQuery = userQuery
                    .OrderBy(u =>
                        u.user.Role == "Homeowner" ? 0 :
                        u.user.Role == "Staff" ? 1 :
                        u.user.Role == "Admin" ? 2 : 3
                    )
                    .ThenBy(u => u.user.Id);

                result = userQuery.Select(u => new
                {
                    u.user.Id,
                    FullName = char.ToUpper(u.info.Firstname[0]) + u.info.Firstname.Substring(1).ToLower() + " " +
                               char.ToUpper(u.info.Lastname[0]) + u.info.Lastname.Substring(1).ToLower(),
                    u.user.Email,
                    u.info.PhoneNumber,
                    u.info.Address,
                    u.user.Status,
                    u.user.Role,
                    DateRegistered = u.user.DateRegistered.ToString("MM/dd/yyyy hh:mm tt").ToUpper()
                }).ToList<object>();
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

    public IActionResult Settings()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
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
        };
        var user = await _context.User_Accounts
         .Join(_context.User_Info,
             u => u.Id,
             info => info.PersonId,
             (u, info) => new { u, info })
         .Where(u => u.u.Id == userId)
         .Select(u => new
         {
             Profile = u.info.Profile ?? "",
             u.info.Firstname,
             u.info.Lastname,
             u.u.Email,
             Contact = u.info.PhoneNumber,
             u.info.Address
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

        var userInfo = await _context.User_Info
            .FirstOrDefaultAsync(info => info.UserAccountId == userId);

        if (userInfo == null)
        {
            return NotFound();
        }

        var ext = Path.GetExtension(file.FileName);
        var name = $"{char.ToUpper(userInfo.Firstname[0])}{userInfo.Lastname}-{userId}{ext}";
        var savePath = Path.Combine("wwwroot/assets/userprofile", name);
        var relativePath = $"/assets/userprofile/{name}";

        using (var stream = new FileStream(savePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        userInfo.Profile = relativePath;
        await _context.SaveChangesAsync();

        return Ok(new { path = relativePath });
    }

    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userIdStr = HttpContext.Request.Cookies["Id"];
        if (!int.TryParse(userIdStr, out int userId))
            return Unauthorized();

        // Join to get User_Info
        var userInfo = await _context.User_Info.FirstOrDefaultAsync(info => info.UserAccountId == userId);
        if (userInfo == null) return NotFound();

        // Email check (still in User_Accounts)
        var existingEmail = await _context.User_Accounts
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != userId);
        if (existingEmail != null)
            return Conflict(new { message = "Email already in use by another user." });

        // Full name check (in User_Info)
        var nameExists = await _context.User_Info
            .FirstOrDefaultAsync(info =>
                info.Firstname.ToLower() == request.Firstname.ToLower() &&
                info.Lastname.ToLower() == request.Lastname.ToLower() &&
                info.UserAccountId != userId);
        if (nameExists != null)
            return Conflict(new { message = "Another user already has the same full name." });

        // Contact check (in User_Info)
        var contactExists = await _context.User_Info
            .FirstOrDefaultAsync(info =>
                info.PhoneNumber == request.Contact && info.UserAccountId != userId);
        if (contactExists != null)
            return Conflict(new { message = "Contact already in use." });

        // Update User_Info
        userInfo.Firstname = request.Firstname;
        userInfo.Lastname = request.Lastname;
        userInfo.Address = request.Address;
        userInfo.PhoneNumber = request.Contact;

        // Also update User_Accounts (email only)
        var userAccount = await _context.User_Accounts.FirstOrDefaultAsync(u => u.Id == userId);
        if (userAccount == null) return NotFound();

        userAccount.Email = request.Email;

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
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("Landing");
        }
        return View();
    }
}
