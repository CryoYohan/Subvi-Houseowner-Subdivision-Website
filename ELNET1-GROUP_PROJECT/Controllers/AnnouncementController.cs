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

    // Fetch all announcements
    public IActionResult Announcements()
    {
        var announcements = _context.Announcement
            .OrderByDescending(a => a.DatePosted)
            .ToList();
        return View(announcements);
    }

    
}
