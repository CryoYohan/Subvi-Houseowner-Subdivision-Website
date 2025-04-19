using Microsoft.AspNetCore.Mvc;
using System.Linq;
using ELNET1_GROUP_PROJECT.Data;
using ELNET1_GROUP_PROJECT.Models;
using ELNET1_GROUP_PROJECT.Controllers;
using Microsoft.EntityFrameworkCore;

namespace YourNamespace.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly MyAppDBContext _context;

        public NotificationsController(MyAppDBContext context)
        {
            _context = context;
        }

        // Get unread notifications for a specific user
        [HttpGet("{userId}/{selectedType?}")]
        public IActionResult GetAllHomeownerNotifications(int userId, string? selectedType)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId && n.TargetRole == "Homeowner");

            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                query = query.Where(n => n.Type == selectedType);
            }
            else
            {
                query = query.OrderByDescending(n => n.DateCreated).Take(25);
            }

            var notifications = query
                .OrderByDescending(n => n.DateCreated)
                .ToList();

            return Ok(notifications);
        }

        // Get a unread notification by ID counting
        [HttpGet("unread-count/{userId}")]
        public IActionResult GetUnreadCount(int userId)
        {
            var count = _context.Notifications
                .Count(n => n.UserId == userId && (n.IsRead == false || n.IsRead == null));

            return Ok(new { count });
        }

        //Get unread notifications every notification type
        [HttpGet("type/unread-count/{userId}")]
        public IActionResult GetUnreadTypeCount(int userId)
        {
            var counts = _context.Notifications
                .Where(n => n.UserId == userId && (n.IsRead == false || n.IsRead == null))
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            return Ok(counts);
        }


        // Mark specific notification as read
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
                return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Mark a all notification as read
        [HttpPut("mark-all-read/{userId}/{selectedType?}")]
        public IActionResult MarkAllAsRead(int userId, string? selectedType)
        {
            var unread = _context.Notifications
                .Where(n => n.UserId == userId &&
                            (n.IsRead == false || n.IsRead == null));

            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                unread = unread.Where(n => n.Type == selectedType);
            }

            var unreadList = unread.ToList();

            foreach (var n in unreadList)
            {
                n.IsRead = true;
                n.DateRead = DateTime.UtcNow;
            }

            _context.SaveChanges();

            return Ok();
        }

        // Get all staff notifications
        [HttpGet("staff/{userId}/{selectedType?}")]
        public IActionResult GetAllStaffNotifications(int userId, string? selectedType)
        {
            var query = _context.Notifications
                .Where(n => n.TargetRole == "Staff");

            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                query = query.Where(n => n.Type == selectedType);
            }
            else
            {
                query = query.OrderByDescending(n => n.DateCreated).Take(25);
            }

            var notifications = query
                .OrderByDescending(n => n.DateCreated)
                .ToList();

            var readNotifIds = _context.Notification_Reads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToList();

            var results = notifications.Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Type,
                n.Message,
                n.DateCreated,
                n.Link,
                IsRead = readNotifIds.Contains(n.NotificationId)
            });

            return Ok(results);
        }

        // Count unread notifications
        [HttpGet("staff/unread-count/{userId}")]
        public IActionResult GetStaffUnreadCount(int userId)
        {
            var totalNotifIds = _context.Notifications
                .Where(n => n.TargetRole == "Staff")
                .Select(n => n.NotificationId)
                .ToList();

            var readNotifIds = _context.Notification_Reads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToList();

            var unreadCount = totalNotifIds.Except(readNotifIds).Count();

            return Ok(new { count = unreadCount });
        }

        //Get unread notifications every notification type in staff
        [HttpGet("staff/type/unread-count/{staffUserId}")]
        public IActionResult GetStaffUnreadTypeCount(int staffUserId)
        {
            var counts = _context.Notifications
                .Where(n => n.TargetRole == "Staff" &&
                            !_context.Notification_Reads
                                .Any(r => r.NotificationId == n.NotificationId && r.UserId == staffUserId))
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            return Ok(counts);
        }

        // Mark specific notification as read
        [HttpPut("staff/mark-read/{userId}/{notificationId}")]
        public async Task<IActionResult> StaffMarkAsRead(int userId, int notificationId)
        {
            var exists = await _context.Notifications
                .AnyAsync(n => n.NotificationId == notificationId && n.TargetRole == "Staff");

            if (!exists) return NotFound();

            var alreadyRead = await _context.Notification_Reads
                .AnyAsync(r => r.UserId == userId && r.NotificationId == notificationId);

            if (!alreadyRead)
            {
                _context.Notification_Reads.Add(new NotificationReads
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    DateRead = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // Mark all as read
        [HttpPut("staff/mark-all-read/{userId}/{selectedType?}")]
        public async Task<IActionResult> StaffMarkAllAsRead(int userId, string? selectedType)
        {
            var query = _context.Notifications
                .Where(n => n.TargetRole == "Staff");

            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                query = query.Where(n => n.Type == selectedType);
            }

            var allNotifIds = await query
                .Select(n => n.NotificationId)
                .ToListAsync();

            var alreadyRead = await _context.Notification_Reads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            var unreadNotifIds = allNotifIds.Except(alreadyRead).ToList();

            foreach (var notifId in unreadNotifIds)
            {
                _context.Notification_Reads.Add(new NotificationReads
                {
                    UserId = userId,
                    NotificationId = notifId,
                    DateRead = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // Get all admin notifications
        [HttpGet("admin/{userId}/{selectedType?}")]
        public IActionResult GetAllAdminNotifications(int userId, string? selectedType)
        {
            var query = _context.Notifications
                .Where(n => n.TargetRole == "Admin");

            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                query = query.Where(n => n.Type == selectedType);
            }
            else
            {
                query = query.OrderByDescending(n => n.DateCreated).Take(25);
            }

            var notifications = query
                .OrderByDescending(n => n.DateCreated)
                .ToList();

            var readNotifIds = _context.Notification_Reads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToList();

            var results = notifications.Select(n => new
            {
                n.NotificationId,
                n.Title,
                n.Type,
                n.Message,
                n.DateCreated,
                n.Link,
                IsRead = readNotifIds.Contains(n.NotificationId)
            });

            return Ok(results);
        }

        // Count unread notifications
        [HttpGet("admin/unread-count/{userId}")]
        public IActionResult GetAdminUnreadCount(int userId)
        {
            var totalNotifIds = _context.Notifications
                .Where(n => n.TargetRole == "Admin")
                .Select(n => n.NotificationId)
                .ToList();

            var readNotifIds = _context.Notification_Reads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToList();

            var unreadCount = totalNotifIds.Except(readNotifIds).Count();

            return Ok(new { count = unreadCount });
        }

        //Get unread notifications every notification type in admin
        [HttpGet("admin/type/unread-count/{adminUserId}")]
        public IActionResult GetAdminUnreadTypeCount(int adminUserId)
        {
            var counts = _context.Notifications
                .Where(n => n.TargetRole == "Admin" &&
                            !_context.Notification_Reads
                                .Any(r => r.NotificationId == n.NotificationId && r.UserId == adminUserId))
                .GroupBy(n => n.Type)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList();

            return Ok(counts);
        }

        // Mark specific notification as read
        [HttpPut("admin/mark-read/{userId}/{notificationId}")]
        public async Task<IActionResult> AdminMarkAsRead(int userId, int notificationId)
        {
            var exists = await _context.Notifications
                .AnyAsync(n => n.NotificationId == notificationId && n.TargetRole == "Admin");

            if (!exists) return NotFound();

            var alreadyRead = await _context.Notification_Reads
                .AnyAsync(r => r.UserId == userId && r.NotificationId == notificationId);

            if (!alreadyRead)
            {
                _context.Notification_Reads.Add(new NotificationReads
                {
                    UserId = userId,
                    NotificationId = notificationId,
                    DateRead = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            return NoContent();
        }

        // Mark all as read
        [HttpPut("admin/mark-all-read/{userId}/{selectedType?}")]
        public async Task<IActionResult> AdminMarkAllAsRead(int userId, string? selectedType)
        {
            var query = _context.Notifications
                .Where(n => n.TargetRole == "Admin");

            if (!string.IsNullOrWhiteSpace(selectedType))
            {
                query = query.Where(n => n.Type == selectedType);
            }

            var allNotifIds = await query
                .Select(n => n.NotificationId)
                .ToListAsync();

            var alreadyRead = await _context.Notification_Reads
                .Where(r => r.UserId == userId)
                .Select(r => r.NotificationId)
                .ToListAsync();

            var unreadNotifIds = allNotifIds.Except(alreadyRead).ToList();

            foreach (var notifId in unreadNotifIds)
            {
                _context.Notification_Reads.Add(new NotificationReads
                {
                    UserId = userId,
                    NotificationId = notifId,
                    DateRead = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
