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
        ViewData["Layout"] = "_AdminLayout";  // Specify the custom layout for the Admin section
    }

    public IActionResult Index()
    {
        // Get all users from the database to display in the table
        var users = _context.User_Accounts.ToList();
        return View(users);  // Pass the users list to the view
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
                    return RedirectToAction("Index");
                }

                // Add the new user account to the database
                _context.User_Accounts.Add(model);
                _context.SaveChanges();

                // Pass a success message to TempData
                TempData["SuccessMessage"] = "User account added successfully!";

                // Redirect to the Index action to reload the page with the updated list of users
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Pass an error message to TempData
                TempData["ErrorMessage"] = "Error adding user account: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // If validation fails, redisplay the form with the existing data
        return View("Index", model);
    }

    // Function to delete all users
    public IActionResult DeleteAllUsers()
    {
        var allUsers = _context.User_Accounts.ToList(); // Retrieve all users

        _context.User_Accounts.RemoveRange(allUsers); // Remove all users
        _context.SaveChanges(); // Save changes to the database

        // Optionally, you can add a success message here or redirect
        return RedirectToAction("Index"); // Or display a confirmation
    }

    // Edit user functionality
    public IActionResult EditUser(int id)
    {
        var user = _context.User_Accounts.FirstOrDefault(u => u.Id == id);
        if (user == null)
        {
            TempData["ErrorMessage"] = "User not found.";
            return RedirectToAction("Index");
        }

        return View(user);  // Pass the user object to the view for editing
    }

    // POST: /Admin/EditUser
    [HttpPost]
    public IActionResult EditUser(User_Account model)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Find the existing user by ID
                var existingUser = _context.User_Accounts.FirstOrDefault(u => u.Id == model.Id);
                if (existingUser == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToAction("Index");
                }

                // Check if the email is being updated and if the new email already exists
                if (existingUser.Email != model.Email && _context.User_Accounts
                        .Any(u => u.Email == model.Email))
                {
                    TempData["ErrorMessage"] = "User already exists with this email.";
                    return RedirectToAction("Index");
                }

                // Update the user data
                existingUser.Firstname = model.Firstname;
                existingUser.Lastname = model.Lastname;
                existingUser.Email = model.Email;
                existingUser.Role = model.Role;
                existingUser.Address = model.Address;
                existingUser.PhoneNumber = model.PhoneNumber;

                // Save changes to the database
                _context.SaveChanges();

                TempData["SuccessMessage"] = "User updated successfully!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating user: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // If validation fails, redisplay the form
        return View(model);
    }
}
