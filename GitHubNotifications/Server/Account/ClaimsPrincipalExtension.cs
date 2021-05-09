using System.Security.Claims;
using GitHubNotifications.Shared;

namespace GitHubNotifications.Server
{
    public static class ClaimsPrincipalExtension
    {
        public static string GetGitHubLogin(this ClaimsPrincipal user)
        {
            return user.FindFirst(c => c.Type == ClaimConstants.Login)?.Value;
        }
    }
}
