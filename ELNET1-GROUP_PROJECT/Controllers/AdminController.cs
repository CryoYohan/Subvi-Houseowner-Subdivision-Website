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

public class AdminController : Controller
{
    private readonly MyAppDBContext _context;

    public AdminController(MyAppDBContext context)
    {
        _context = context;
        ViewData["Layout"] = "_AdminLayout"; 
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
                    where r.Status == "Approved" || r.Status == "Declined"
                    select new ReservationViewModel
                    {
                        Id = r.ReservationId,
                        FacilityName = char.ToUpper(f.FacilityName[0]) + f.FacilityName.Substring(1),
                        RequestedBy = char.ToUpper(u.Firstname[0]) + u.Firstname.Substring(1) + " " + char.ToUpper(u.Lastname[0]) + u.Lastname.Substring(1),
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
        // Get all users from the database to display in the table
        var users = _context.User_Accounts.ToList();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View(users);
    }

    // POST: /Admin/AddUserAccount
    [HttpPost]
    public IActionResult AddUserAccount(User_Account model)
    {
        if (ModelState.IsValid)
        {
            RefreshJwtCookies();
            try
            {
                // Check if the email already exists
                var existingUser = _context.User_Accounts
                    .FirstOrDefault(u => u.Email == model.Email);

                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = "User already exists with this email.";
                    return RedirectToAction("HomeownerStaffAccounts");
                }

                // Hash the password before saving it
                string fullname = model.Firstname + " " + model.Lastname;
                string username = model.Email;
                string originalPassword = model.Password;
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Add the new user to the database
                _context.User_Accounts.Add(model);
                _context.SaveChanges();

                // Send email only if Role is "Homeowner"
                if (model.Role == "Homeowner")
                {
                    var emailSent = SendEmail(model.Email, fullname, username, originalPassword);
                    /*
                    if (!emailSent)
                    {
                        TempData["ErrorMessage"] = "User registered, but failed to send email.";
                        return RedirectToAction("HomeownerStaffAccounts");
                    }
                    */
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

        // If validation fails, redisplay the form with validation errors
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

    // POST: /Admin/EditUser
    [HttpPost]
    public IActionResult EditUser(User_Account model)
    {
        RefreshJwtCookies();
        // Remove the Password field from model validation
        ModelState.Remove("Password");

        if (!ModelState.IsValid)
        {
            // Log validation errors
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            TempData["ErrorMessage"] = "Validation failed: " + string.Join(", ", errors);
            return RedirectToAction("HomeownerStaffAccounts");
        }

        try
        {
            // Find the existing user by ID
            var existingUser = _context.User_Accounts.FirstOrDefault(u => u.Id == model.Id);
            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Check if the email is being updated and if the new email already exists
            if (existingUser.Email != model.Email && _context.User_Accounts
                    .Any(u => u.Email == model.Email))
            {
                TempData["ErrorMessage"] = "User already exists with this email.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Update the user data
            existingUser.Firstname = model.Firstname;
            existingUser.Lastname = model.Lastname;
            existingUser.Email = model.Email;
            existingUser.Role = model.Role;
            existingUser.Address = model.Address;
            existingUser.PhoneNumber = model.PhoneNumber;

            // Conditionally update the password with BCrypt
            if (!string.IsNullOrEmpty(model.Password))
            {
                existingUser.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);
            }

            // Save changes to the database
            _context.SaveChanges();

            TempData["SuccessMessage"] = "User updated successfully!";
            return RedirectToAction("HomeownerStaffAccounts");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to update user info. Please try again later. ";
            return RedirectToAction("HomeownerStaffAccounts");
        }
    }

    // Function to delete a specific user by ID
    public IActionResult DeleteUser(int id)
    {
        RefreshJwtCookies();
        try
        {
            // Find the user by ID
            var user = _context.User_Accounts.FirstOrDefault(u => u.Id == id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("HomeownerStaffAccounts");
            }

            // Remove the specific user from the database
            _context.User_Accounts.Remove(user);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "User deleted successfully!";
            return RedirectToAction("HomeownerStaffAccounts");
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to delete user. Please try again later. ";
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

    [HttpGet]
    public async Task<IActionResult> GetBillPayments(int billId)
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
    public IActionResult AddAnnouncement(string title, string description)
    {
        RefreshJwtCookies();
        var userIdStr = Request.Cookies["Id"];
        int userId = int.Parse(userIdStr);
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
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Announcement added successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.InnerException?.Message;
        }
        return RedirectToAction("Announcements");
    }

    // Edit an existing announcement
    [HttpPost]
    public IActionResult EditAnnouncement(int id, string title, string description)
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
                TempData["SuccessMessage"] = "Announcement updated successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Announcement not found.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to update announcement. Please try again.";
        }
        return RedirectToAction("Announcements");
    }

    // Delete an announcement
    [HttpPost]
    public IActionResult DeleteAnnouncement(int id)
    {
        RefreshJwtCookies();
        try
        {
            var announcement = _context.Announcement.Find(id);
            if (announcement != null)
            {
                _context.Announcement.Remove(announcement);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Announcement deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Announcement not found.";
            }
        }
        catch (Exception ex)
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

    public IActionResult Reports()
    {
        RefreshJwtCookies();
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
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
}
