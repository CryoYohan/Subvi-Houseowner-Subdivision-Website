using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        return View();  // This will use _AdminLayout.cshtml
    }
    // POST: /Admin/AddUserAccount
    [HttpPost]
    public IActionResult AddUserAccount(User_Account model)
    {
        if (ModelState.IsValid)
        {
            // Add the new user account to the database
            _context.User_Accounts.Add(model);
            _context.SaveChanges();

            // Optionally, redirect to another action (like a confirmation page)
            return RedirectToAction("Index");
        }
        // If validation fails, redisplay the form with the existing data
        return View("Index", model);
    }
}
