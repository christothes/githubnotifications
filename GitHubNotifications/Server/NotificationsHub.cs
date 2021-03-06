using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace GitHubNotifications
{
    // [Authorize]
    public class NotificationsHub : Hub
    {
        private readonly ILogger<NotificationsHub> _logger;

        public NotificationsHub(ILogger<NotificationsHub> logger)
        {
            _logger = logger;
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }
    }
}
