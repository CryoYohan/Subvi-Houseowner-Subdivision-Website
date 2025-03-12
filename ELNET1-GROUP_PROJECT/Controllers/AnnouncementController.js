using Microsoft.AspNetCore.Mvc;
using ELNET1_GROUP_PROJECT.Models;
using System;
using System.Linq;

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
            .OrderByDescending(a => a.DATE_POSTED)
            .ToList();
        return View(announcements);
    }

    // Add a new announcement
    [HttpPost]
    public IActionResult AddAnnouncement(string title, string description, int userId)
    {
        var newAnnouncement = new Announcement
        {
            TITLE = title,
                DESCRIPTION = description,
                DATE_POSTED = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                USER_ID = userId
        };

        _context.Announcements.Add(newAnnouncement);
        _context.SaveChanges();
        return Ok(new { message = "Announcement added successfully!" });
    }

    // Edit an existing announcement
    [HttpPost]
    public IActionResult EditAnnouncement(int id, string title, string description)
    {
        var announcement = _context.Announcements.Find(id);
        if (announcement != null) {
            announcement.TITLE = title;
            announcement.DESCRIPTION = description;
            _context.SaveChanges();
        }
        return Ok(new { message = "Announcement updated successfully!" });
    }

    // Delete an announcement
    [HttpPost]
    public IActionResult DeleteAnnouncement(int id)
    {
        var announcement = _context.Announcements.Find(id);
        if (announcement != null) {
            _context.Announcements.Remove(announcement);
            _context.SaveChanges();
        }
        return Ok(new { message = "Announcement deleted successfully!" });
    }
}
