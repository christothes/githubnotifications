using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace GitHubNotifications.Pages
{
    public class Login : PageModel
    {
        public async Task OnGet(string redirectUri)
        {
            await HttpContext.ChallengeAsync("GitHub", new AuthenticationProperties
            {
                RedirectUri = redirectUri
            });
        }
    }
}