using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ELNET1_GROUP_PROJECT.Models;
using ELNET1_GROUP_PROJECT.Data;
using System;
using System.Linq;
using System.Text.Json;
using Azure.Core;
using Microsoft.AspNetCore.SignalR;
using ELNET1_GROUP_PROJECT.SignalR;
using System.Globalization;

[Route("api")]
[ApiController]
public class HomeDashboardController : ControllerBase
{
    private readonly MyAppDBContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public HomeDashboardController(MyAppDBContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpGet("announcement")]
    public async Task<IActionResult> GetAnnouncements()
    {
        var announcements = await (from announcement in _context.Announcement
                                   join user in _context.User_Accounts on announcement.UserId equals user.Id
                                   join info in _context.User_Info on user.Id equals info.UserAccountId
                                   select new
                                   {
                                       announcement.AnnouncementId,
                                       announcement.Title,
                                       announcement.Description,
                                       announcement.DatePosted,
                                       PostedBy = info.Firstname + " " + info.Lastname // Combine First and Last Name
                                   }).ToListAsync();

        return Ok(announcements);
    }

    [HttpGet("polls")]
    public async Task<IActionResult> GetPolls()
    {
        var userIdStr = Request.Cookies["Id"];
        if (string.IsNullOrEmpty(userIdStr)) return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);
        var today = DateTime.Today;

        // Step 1: Load all active polls from DB (Status = true)
        var allPolls = await _context.Poll
            .Where(p => p.Status && (p.IsDeleted == false))
            .OrderByDescending(p => p.PollId)
            .ToListAsync();

        var pollsToUpdate = new List<Poll>();
        var pollsToNotify = new List<Notification>();

        foreach (var poll in allPolls)
        {
            if (poll.IsDeleted == true)
                continue;

            if (DateTime.TryParseExact(poll.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) &&
                DateTime.TryParseExact(poll.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate))
            {
                // Check if poll status needs to be updated
                if (today < startDate && poll.Status == true)
                {
                    poll.Status = false; // Set to inactive
                    pollsToUpdate.Add(poll);

                    // Prepare notification for poll end
                    var endedNotification = new Notification
                    {
                        Title = "Poll Ended",
                        Message = $"The poll '{poll.Title}' has ended. You may no longer submit your vote.",
                        Type = "Poll",
                        IsRead = false,
                        DateCreated = DateTime.Now,
                        TargetRole = "Homeowner",
                        UserId = null, // Homeowners will be notified
                        Link = "/home/dashboard"
                    };
                    pollsToNotify.Add(endedNotification);
                }
                else if (today >= startDate && today <= endDate && poll.Status == false)
                {
                    poll.Status = true; // Set to active
                    pollsToUpdate.Add(poll);

                    // Prepare notification for poll activation
                    var activatedNotification = new Notification
                    {
                        Title = "Poll Now Active",
                        Message = $"The poll '{poll.Title}' is now active. You may submit your vote.",
                        Type = "Poll",
                        IsRead = false,
                        DateCreated = DateTime.Now,
                        TargetRole = "Homeowner",
                        UserId = null, // Homeowners will be notified
                        Link = "/home/dashboard"
                    };
                    pollsToNotify.Add(activatedNotification);
                }
            }
        }

        if (pollsToUpdate.Any())
        {
            // Update poll statuses in the database in bulk
            _context.Poll.UpdateRange(pollsToUpdate);
            await _context.SaveChangesAsync();

            // Send notifications for status changes
            _context.Notifications.AddRange(pollsToNotify);
            await _context.SaveChangesAsync();

            // Get list of homeowners for notification push
            var homeowners = await _context.User_Accounts.Where(u => u.Role == "Homeowner").ToListAsync();

            foreach (var homeowner in homeowners)
            {
                // Send notification to each homeowner for the status change
                foreach (var notification in pollsToNotify)
                {
                    notification.UserId = homeowner.Id;
                    await _hubContext.Clients.User(homeowner.Id.ToString()).SendAsync("ReceiveNotification", notification);
                }
            }

            // Optionally, notify staff and admin groups (if needed)
            var staffNotification = new Notification
            {
                Title = "Poll Status Update",
                Message = $"Some polls have been activated or ended. Please review.",
                Type = "Poll",
                IsRead = false,
                DateCreated = DateTime.Now,
                TargetRole = "Staff",
                UserId = null,
                Link = "/staff/polls"
            };

            var adminNotification = new Notification
            {
                Title = "Poll Status Update",
                Message = $"Some polls have been activated or ended. Please review.",
                Type = "Poll",
                IsRead = false,
                DateCreated = DateTime.Now,
                TargetRole = "Admin",
                UserId = null,
                Link = "/admin/poll"
            };

            _context.Notifications.Add(staffNotification);
            _context.Notifications.Add(adminNotification);
            await _context.SaveChangesAsync();

            // Notify staff and admin via SignalR (if needed)
            await _hubContext.Clients.Group("staff").SendAsync("ReceiveNotification", staffNotification);
            await _hubContext.Clients.Group("admin").SendAsync("ReceiveNotification", adminNotification);
        }

        // Step 2: Filter by valid date range in memory for active polls only
        var polls = allPolls
            .Where(p =>
                DateTime.TryParseExact(p.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) &&
                DateTime.TryParseExact(p.EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate) &&
                today >= startDate && today <= endDate)
            .Select(p => new
            {
                p.PollId,
                p.Title,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.Status,
                VotedChoice = _context.Vote
                    .Where(v => v.PollId == p.PollId && v.UserId == userId)
                    .Join(_context.Poll_Choice,
                        vote => vote.ChoiceId,
                        choice => choice.ChoiceId,
                        (vote, choice) => choice.Choice)
                    .FirstOrDefault()
            })
            .ToList();

        return Ok(polls);
    }

    [HttpGet("polls/{pollId}/choices")]
    public async Task<IActionResult> GetPollChoices(int pollId)
    {
        var userIdStr = Request.Cookies["Id"];
        if (string.IsNullOrEmpty(userIdStr)) return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);

        // Fetch all choices for the given poll
        var choices = await _context.Poll_Choice
            .Where(pc => pc.PollId == pollId)
            .ToListAsync();

        // Check if the user has already voted for this poll
        var userVote = await _context.Vote
            .Where(v => v.PollId == pollId && v.UserId == userId)
            .FirstOrDefaultAsync();

        // Map the Poll_Choice entities to PollChoiceDTO and set the IsVoted property
        var pollChoicesDTO = choices.Select(c => new PollChoiceDTO
        {
            ChoiceId = c.ChoiceId,
            Choice = c.Choice,
            IsVoted = userVote != null && userVote.ChoiceId == c.ChoiceId  // Mark the voted choice
        }).ToList();

        return Ok(pollChoicesDTO);
    }

    [HttpPost("polls/vote")]
    public async Task<IActionResult> CastVote([FromBody] VoteRequest voteRequest)
    {
        var userIdStr = Request.Cookies["Id"];
        if (string.IsNullOrEmpty(userIdStr))
            return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);

        // Check if the user has already voted
        var existingVote = await _context.Vote
            .FirstOrDefaultAsync(v => v.PollId == voteRequest.PollId && v.UserId == userId);

        if (existingVote != null)
        {
            return BadRequest(new { message = "You have already voted for this poll." });
        }

        // Fetch poll info (to get the title)
        var poll = await _context.Poll.FirstOrDefaultAsync(p => p.PollId == voteRequest.PollId);
        if (poll == null)
        {
            return NotFound(new { message = "Poll not found." });
        }

        // Create the vote
        var vote = new Vote
        {
            PollId = voteRequest.PollId,
            ChoiceId = voteRequest.ChoiceId,
            UserId = userId,
            VoteDate = DateTime.Now
        };

        _context.Vote.Add(vote);
        await _context.SaveChangesAsync();

        // Create a notification for all homeowners
        var notification = new Notification
        {
            UserId = null, // null means broadcast to all homeowners
            TargetRole = "Homeowner",
            Type = "Poll",
            Title = $"{poll.Title} Poll Voted",
            Message = $"You voted to the {poll.Title} Poll.",
            IsRead = false,
            DateCreated = DateTime.Now,
            Link = "/home/dashboard"
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Real-time notification to all homeowners (you may need to adjust targeting logic)
        await _hubContext.Clients.Group("Homeowner").SendAsync("ReceiveNotification", notification);

        return Ok(new { message = "Vote casted successfully." });
    }

    // PUT: api/polls/vote
    [HttpPut("polls/vote")]
    public async Task<IActionResult> ChangeVote([FromBody] VoteRequest voteRequest)
    {
        var userIdStr = Request.Cookies["Id"];
        if (string.IsNullOrEmpty(userIdStr))
            return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);

        var existingVote = await _context.Vote
            .FirstOrDefaultAsync(v => v.PollId == voteRequest.PollId && v.UserId == userId);

        if (existingVote == null)
        {
            return BadRequest(new { message = "You haven't voted yet." });
        }

        // Fetch poll info to include the title in the notification
        var poll = await _context.Poll.FirstOrDefaultAsync(p => p.PollId == voteRequest.PollId);
        if (poll == null)
        {
            return NotFound(new { message = "Poll not found." });
        }

        // Update the existing vote
        existingVote.ChoiceId = voteRequest.ChoiceId;
        existingVote.VoteDate = DateTime.Now;

        _context.Vote.Update(existingVote);
        await _context.SaveChangesAsync();

        // Create a notification for all homeowners
        var notification = new Notification
        {
            UserId = null,
            TargetRole = "Homeowner",
            Type = "Poll",
            Title = "Poll Vote Changed",
            Message = $"Your vote for the poll {poll.Title} has been updated.",
            IsRead = false,
            DateCreated = DateTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Send real-time notification to homeowners (adjust group logic as needed)
        await _hubContext.Clients.Group("Homeowner").SendAsync("ReceiveNotification", notification);

        return Ok(new { message = "Vote updated successfully." });
    }

    [HttpGet("polls/vote-status/{pollId}")]
    public async Task<IActionResult> GetVoteStatus(int pollId)
    {
        try
        {
            var userIdStr = Request.Cookies["Id"];
            if (string.IsNullOrEmpty(userIdStr)) return BadRequest(new { message = "User not logged in" });

            int userId = int.Parse(userIdStr);

            // Check if the user has voted for the given poll
            var vote = await _context.Vote
                .FirstOrDefaultAsync(v => v.PollId == pollId && v.UserId == userId);

            // Return the choiceId if the user has voted, otherwise return null
            return Ok(new { choiceId = vote?.ChoiceId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

    [HttpGet("polls/{pollId}/percentages")]
    public async Task<IActionResult> GetAllVotePercentagesForPoll(int pollId)
    {
        try
        {
            // Get all choices for this poll
            var choices = await _context.Poll_Choice
                .Where(pc => pc.PollId == pollId)
                .ToListAsync();

            if (choices == null || choices.Count == 0)
                return NotFound(new { message = "No choices found for this poll." });

            // Total voters with active 'Homeowner' or 'Staff' roles
            var totalVoters = await _context.User_Accounts
                .Where(u => (u.Role == "Homeowner" || u.Role == "Staff") && u.Status == "ACTIVE")
                .CountAsync();

            // For each choice, count votes
            var result = new List<object>();
            foreach (var choice in choices)
            {
                // Count the number of votes for this choice
                var voteCount = await _context.Vote
                    .Where(v => v.ChoiceId == choice.ChoiceId)
                    .CountAsync();

                // Calculate the percentage based on total voters
                double percentage = totalVoters > 0 ? ((double)voteCount / totalVoters) * 100 : 0;

                result.Add(new
                {
                    choiceId = choice.ChoiceId,
                    choice = choice.Choice,
                    pollId = pollId,
                    voteCount,
                    percentage
                });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Something went wrong.", error = ex.Message });
        }
    }

    [HttpGet("getevents")]
    public IActionResult GetEventsByDate(string date)
    {
        if (!DateTime.TryParse(date, out DateTime selectedDate))
        {
            return BadRequest("Invalid date format.");
        }

        var events = _context.Event_Calendar
            .Where(e => e.DateTime.Date == selectedDate.Date)
            .OrderByDescending(e => e.DateTime)
            .Select(e => new
            {
                e.Description,
                DateTime = e.DateTime.ToString("MM/dd/yyyy")
            })
            .ToList();

        return Ok(events);
    }

}
