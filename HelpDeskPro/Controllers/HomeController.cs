using HelpDeskPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    public class HomeController : Controller
    {
        // Public landing page. Logged-in users go straight to their own dashboard.
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                if (User.IsInRole(Roles.Admin))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }
                if (User.IsInRole(Roles.Agent))
                {
                    return RedirectToAction("Index", "AgentDashboard");
                }
                return RedirectToAction("Dashboard", "Customer");
            }
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error(string id)
        {
            ViewBag.CorrelationId = id;
            return View();
        }
    }
}
