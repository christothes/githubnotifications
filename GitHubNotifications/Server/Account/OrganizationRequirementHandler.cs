using Microsoft.AspNetCore.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace GitHubNotifications.Server
{
    public class OrganizationRequirementHandler : IAuthorizationHandler
    {
        public Task HandleAsync(AuthorizationHandlerContext context)
        {
            foreach (var requirement in context.Requirements)
            {
                if (requirement is OrganizationRequirement orgRequirement)
                {
                    var claim = context.User.FindFirst("urn:github:orgs");
                    if (claim != null)
                    {
                        var userOrganizations = claim.Value.Split(",");
                        if (userOrganizations.Any(userOrg => orgRequirement.RequiredOrganizations.Contains(userOrg, StringComparer.OrdinalIgnoreCase)))
                        {
                            context.Succeed(requirement);
                        }
                    }
                }
            }
            return Task.CompletedTask;
        }
    }
}
