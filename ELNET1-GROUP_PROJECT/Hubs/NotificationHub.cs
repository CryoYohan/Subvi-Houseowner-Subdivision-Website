using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ELNET1_GROUP_PROJECT.SignalR
{
    public class NotificationHub : Hub
    {
        // Sends a notification to a specific user
        public async Task SendNotification(int userId, string title, string message, string role)
        {
            var notification = new
            {
                UserId = userId,
                Title = title,
                Message = message,
                DateCreated = DateTime.UtcNow
            };

            // Send to the specific user (homeowner)
            if (role == "Homeowner")
            {
                await Clients.User(userId.ToString()).SendAsync("ReceiveNotification", notification);
            }
            // Send to all users with the "staff" role (Staff only)
            else if (role == "Staff")
            {
                await Clients.Group("staff").SendAsync("ReceiveNotification", notification);
            }
        }

        // Adding a staff group to allow notifications only for staff members
        public async Task AddToStaffGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
        }

        // Broadcast a notification to all users (not restricted by role)
        public async Task BroadcastNotification(string title, string message)
        {
            var notification = new
            {
                Title = title,
                Message = message,
                DateCreated = DateTime.UtcNow
            };

            await Clients.All.SendAsync("ReceiveNotification", notification);
        }
    }
}
