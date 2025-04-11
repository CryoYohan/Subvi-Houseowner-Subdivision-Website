using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ELNET1_GROUP_PROJECT.Models;
using System;
using System.Linq;
using ELNET1_GROUP_PROJECT.Data;

[Route("api")]
[ApiController]
public class AnnouncementController : ControllerBase
{
    private readonly MyAppDBContext _context;

    public AnnouncementController(MyAppDBContext context)
    {
        _context = context;
    }

    [HttpGet("announcement")]
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

    [HttpGet("polls")]
    public async Task<IActionResult> GetPolls()
    {
        var userIdStr = Request.Cookies["Id"];
        if (string.IsNullOrEmpty(userIdStr)) return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);

        var polls = await _context.Poll
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
        .ToListAsync();

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
        if (string.IsNullOrEmpty(userIdStr)) return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);

        var existingVote = await _context.Vote
            .Where(v => v.PollId == voteRequest.PollId && v.UserId == userId)
            .FirstOrDefaultAsync();

        if (existingVote != null)
        {
            return BadRequest("You have already voted for this poll.");
        }

        var vote = new Vote
        {
            PollId = voteRequest.PollId,
            ChoiceId = voteRequest.ChoiceId,
            UserId = userId
        };

        _context.Vote.Add(vote);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Vote casted successfully" });
    }

    // PUT: api/polls/vote
    [HttpPut("polls/vote")]
    public async Task<IActionResult> ChangeVote([FromBody] VoteRequest voteRequest)
    {
        var userIdStr = Request.Cookies["Id"];
        if (string.IsNullOrEmpty(userIdStr)) return BadRequest(new { message = "User not logged in" });

        int userId = int.Parse(userIdStr);

        var existingVote = await _context.Vote
            .Where(v => v.PollId == voteRequest.PollId && v.UserId == userId)
            .FirstOrDefaultAsync();

        if (existingVote == null)
        {
            return BadRequest("You haven't voted yet.");
        }

        // Update the existing vote with the new choice
        existingVote.ChoiceId = voteRequest.ChoiceId;
        _context.Vote.Update(existingVote);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Vote updated successfully" });
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

    [HttpGet("polls/vote-percentage/{choiceId}")]
    public async Task<IActionResult> GetVotePercentageForChoice(int choiceId)
    {
        try
        {
            var pollId = await _context.Poll_Choice
                .Where(pc => pc.ChoiceId == choiceId)
                .Select(pc => pc.PollId)
                .FirstOrDefaultAsync();

            if (pollId == 0)
                return NotFound(new { message = "Choice not found." });

            // Calculate the total number of voters (e.g., users with an active "Homeowner" role)
            var totalVoters = await _context.User_Accounts
                .Where(u => u.Role == "Homeowner" && u.Status == "ACTIVE")
                .CountAsync();

            // Count the number of votes for the given choiceId
            var choiceVotes = await _context.Vote
                .Where(v => v.ChoiceId == choiceId)
                .CountAsync();

            // Calculate the percentage of votes for this choice
            double percentage = totalVoters > 0 ? ((double)choiceVotes / totalVoters) * 100 : 0;

            return Ok(new
            {
                choiceId,
                pollId,
                totalVoters,
                choiceVotes,
                percentage
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred.", error = ex.Message });
        }
    }

}
