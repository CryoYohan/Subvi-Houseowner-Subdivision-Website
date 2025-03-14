using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Linq;

public class AdminController : Controller
{
    private readonly MyAppDBContext _context;

    public AdminController(MyAppDBContext context)
    {
        _context = context;
        ViewData["Layout"] = "_AdminLayout"; 
    }

    public IActionResult Dashboard()
    {
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
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    public IActionResult HomeownerStaffAccounts()
    {
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
                model.Password = BCrypt.Net.BCrypt.HashPassword(model.Password);

                // Add the new user to the database
                _context.User_Accounts.Add(model);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "User registered successfully!";
                return RedirectToAction("HomeownerStaffAccounts");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to register new account. Try again later. ";
                return RedirectToAction("HomeownerStaffAccounts");
            }
        }

        // If validation fails, redisplay the form with validation errors
        return View(model);
    }

    // Function to delete all users
    public IActionResult DeleteAllUsers()
    {
        var allUsers = _context.User_Accounts.ToList(); // Retrieve all users

        _context.User_Accounts.RemoveRange(allUsers); // Remove all users
        _context.SaveChanges(); // Save changes to the database

        // Optionally, you can add a success message here or redirect
        return RedirectToAction("HomeownerStaffAccounts"); // Or display a confirmation
    }

    // POST: /Admin/EditUser
    [HttpPost]
    public IActionResult EditUser(User_Account model)
    {
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

    public IActionResult BillPayment()
    {
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    public IActionResult PaymentHistory()
    {
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    public IActionResult Services()
    {
        var role = HttpContext.Request.Cookies["UserRole"];
        if (string.IsNullOrEmpty(role) || role != "Admin")
        {
            return RedirectToAction("landing", "Home");
        }
        return View();
    }

    public IActionResult Announcements()
    {
        var announcements = _context.Announcement
            .OrderByDescending(a => a.DatePosted)
            .ToList();
        return View(announcements);
    }

    // Add a new announcement
    [HttpPost]
    public IActionResult AddAnnouncement(string title, string description)
    {   
        var userId = Request.Cookies["Id"];
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

}
