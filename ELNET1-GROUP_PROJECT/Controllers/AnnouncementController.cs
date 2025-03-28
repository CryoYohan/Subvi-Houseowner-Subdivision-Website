using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ELNET1_GROUP_PROJECT.Models;
using System;
using System.Linq;
using ELNET1_GROUP_PROJECT.Data;

[Route("api/[controller]")]
[ApiController]
public class AnnouncementController : ControllerBase
{
    private readonly MyAppDBContext _context;

    public AnnouncementController(MyAppDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnnouncements()
    {
        var announcements = await (from announcement in _context.Announcement
                                   join user in _context.User_Accounts on announcement.UserId equals user.Id
                                   select new
                                   {
                                       announcement.AnnouncementId,
                                       announcement.Title,
                                       announcement.Description,
                                       announcement.DatePosted,
                                       PostedBy = user.Firstname + " " + user.Lastname // Combine First and Last Name
                                   }).ToListAsync();

        return Ok(announcements);
    }
}
