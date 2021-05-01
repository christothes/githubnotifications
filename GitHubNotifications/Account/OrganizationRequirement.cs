using Microsoft.AspNetCore.Authorization;

namespace GitHubNotifications
{
    public class OrganizationRequirement : IAuthorizationRequirement
    {
        public string[] RequiredOrganizations { get; set; }

        public OrganizationRequirement(string[] requiredOrganizations)
        {
            RequiredOrganizations = requiredOrganizations;
        }
    }
}
