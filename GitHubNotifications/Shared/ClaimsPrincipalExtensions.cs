using System.Linq;
using System.Security.Claims;

namespace GitHubNotifications.Shared
{

    public static class ClaimsPrincipalExtensions
    {
        public static string GetGitHubLogin(this ClaimsPrincipal cp) =>
            cp.Claims.FirstOrDefault(c => c.Type == ClaimConstants.Login)?.Value;

        public static string GetGitHubAvatar(this ClaimsPrincipal cp) =>
            cp.Claims.FirstOrDefault(c => c.Type == ClaimConstants.Avatar)?.Value;
        
    }
}