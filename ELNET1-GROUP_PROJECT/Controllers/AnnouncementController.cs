using Microsoft.AspNetCore.Mvc;
using ELNET1_GROUP_PROJECT.Models;
using System;
using System.Linq;
using ELNET1_GROUP_PROJECT.Data;

public class AnnouncementController : Controller
{
    private readonly MyAppDBContext _context;

    public AnnouncementController(MyAppDBContext context)
    {
        _context = context;
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

    // Fetch all announcements
    public IActionResult Announcements()
    {
        RefreshJwtCookies();
        var announcements = _context.Announcement
            .OrderByDescending(a => a.DatePosted)
            .ToList();
        return View(announcements);
    }

    
}
