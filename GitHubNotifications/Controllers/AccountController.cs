using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace GitHubNotifications.Server.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> SignIn(string returnUrl = "/")
        {
            await HttpContext.SignOutAsync();
            if (!Url.IsLocalUrl(returnUrl))
            {
                returnUrl = "/";
            }
            return Challenge(new AuthenticationProperties { RedirectUri = returnUrl }, "GitHub");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Login");
        }
    }
}
