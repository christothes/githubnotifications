using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace GitHubNotifications.Server.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
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
            await HttpContext.SignOutAsync("Cookies");
            return Redirect("/");
        }
    }
}
