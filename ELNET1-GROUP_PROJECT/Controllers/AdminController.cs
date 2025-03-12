using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
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

                // Add the new user account to the database
                _context.User_Accounts.Add(model);
                _context.SaveChanges();

                // Pass a success message to TempData
                TempData["SuccessMessage"] = "User account added successfully!";

                // Redirect to the HomeownerStaffAccounts action to reload the page with the updated list of users
                return RedirectToAction("HomeownerStaffAccounts");
            }
            catch (Exception ex)
            {
                // Pass an error message to TempData
                TempData["ErrorMessage"] = "Error adding user account: " + ex.Message;
                return RedirectToAction("HomeownerStaffAccounts");
            }
        }

        // If validation fails, redisplay the form with the existing data
        return View("HomeownerStaffAccounts", model);
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
        if (ModelState.IsValid)
        {
            try
            {
                // Check if the user exists in the database
                var existingUser = _context.User_Accounts.FirstOrDefault(u => u.Id == model.Id);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("HomeownerStaffAccounts");
                }

                // Check if the new email already exists and belongs to another user
                if (_context.User_Accounts.Any(u => u.Email == model.Email && u.Id != model.Id))
                {
                    TempData["ErrorMessage"] = "Email is already taken.";
                    return RedirectToAction("HomeownerStaffAccounts");
                }

                // Update the user data
                existingUser.Firstname = model.Firstname;
                existingUser.Lastname = model.Lastname;
                existingUser.Role = model.Role;
                existingUser.Address = model.Address;
                existingUser.PhoneNumber = model.PhoneNumber;
                existingUser.Email = model.Email;

                // Save changes
                _context.SaveChanges();

                TempData["SuccessMessage"] = "User updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating user: {ex.Message}";
            }
        }
        else
        {
            TempData["ErrorMessage"] = "Invalid data provided.";
        }

        // Redirect to the main page
        return RedirectToAction("HomeownerStaffAccounts");
    }


    [HttpPost]
    public IActionResult DeleteUser(int id)
    {
        var user = _context.User_Accounts.FirstOrDefault(u => u.Id == id);
        if (user != null)
        {
            _context.User_Accounts.Remove(user);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "User deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "User not found!";
        }

        return RedirectToAction("HomeownerStaffAccounts");
    }

}
