using HelpDeskPro.Services.Common;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    // Small helpers shared by all MVC controllers
    public abstract class AppControllerBase : Controller
    {
        // Who is logged in? (id + role)
        protected UserContext CurrentUser
        {
            get { return UserContext.FromPrincipal(User); }
        }

        // If the service said "not found" or "forbidden", return the matching page.
        // Otherwise returns null and the controller continues.
        protected IActionResult CheckSpecialError(ServiceResult result)
        {
            if (result.Success)
            {
                return null;
            }
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
            }
            if (result.ErrorType == ErrorType.Forbidden)
            {
                return Forbid();
            }
            return null;
        }

        // Shows a green or red message at the top of the next page
        protected void ShowResult(ServiceResult result, string successMessage)
        {
            if (result.Success)
            {
                TempData["Success"] = successMessage;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
    }
}
