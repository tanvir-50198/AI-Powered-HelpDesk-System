using System.Security.Claims;
using HelpDeskPro.Models;
using Microsoft.AspNetCore.Identity;

namespace HelpDeskPro.Services.Common
{
    // Small object that tells a service "who is calling" (id + role).
    // Services use it to enforce ownership and role rules on the server.
    public class UserContext
    {
        public string UserId { get; set; }
        public string Role { get; set; }

        public bool IsAdmin
        {
            get { return Role == Roles.Admin; }
        }

        public bool IsAgent
        {
            get { return Role == Roles.Agent; }
        }

        public bool IsCustomer
        {
            get { return Role == Roles.Customer; }
        }

        // Build a UserContext from the logged-in user (cookie claims)
        public static UserContext FromPrincipal(ClaimsPrincipal principal)
        {
            UserContext context = new UserContext();
            context.UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier);

            if (principal.IsInRole(Roles.Admin))
            {
                context.Role = Roles.Admin;
            }
            else if (principal.IsInRole(Roles.Agent))
            {
                context.Role = Roles.Agent;
            }
            else
            {
                context.Role = Roles.Customer;
            }

            return context;
        }
    }
}
