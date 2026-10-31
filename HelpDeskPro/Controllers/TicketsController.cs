using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using HelpDeskPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    // Web pages for tickets. All rules are enforced by TicketService,
    // so the web pages and the REST API behave exactly the same.
    [Authorize]
    public class TicketsController : AppControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketsController(ITicketService ticketService, UserManager<ApplicationUser> userManager)
        {
            _ticketService = ticketService;
            _userManager = userManager;
        }

        // ---------------- List ----------------
        public async Task<IActionResult> Index([FromQuery] TicketFilter filter)
        {
            UserContext user = CurrentUser;

            TicketListViewModel model = new TicketListViewModel();
            model.Filter = filter;
            model.IsAdmin = user.IsAdmin;
            model.Categories = await _ticketService.GetCategoriesAsync();
            model.Result = await _ticketService.GetTicketsAsync(user, filter);
            return View(model);
        }

        // ---------------- Details ----------------
        public async Task<IActionResult> Details(int id)
        {
            UserContext user = CurrentUser;

            ServiceResult<Ticket> ticketResult = await _ticketService.GetTicketAsync(id, user);
            IActionResult special = CheckSpecialError(ticketResult);
            if (special != null)
            {
                return special;
            }
            Ticket ticket = ticketResult.Data;

            TicketDetailsViewModel model = new TicketDetailsViewModel();
            model.Ticket = ticket;
            model.IsAdmin = user.IsAdmin;
            model.CanManage = user.IsAdmin || (user.IsAgent && ticket.AssignedAgentId == user.UserId);
            model.CanComment = ticket.Status != TicketStatus.Closed;

            ServiceResult<List<TicketHistory>> historyResult = await _ticketService.GetHistoryAsync(id, user);
            if (historyResult.Success)
            {
                model.History = historyResult.Data;
            }

            if (user.IsAdmin)
            {
                IList<ApplicationUser> agents = await _userManager.GetUsersInRoleAsync(Roles.Agent);
                model.Agents = agents.OrderBy(a => a.Email).ToList();
            }

            return View(model);
        }

        // ---------------- Create (customers only) ----------------
        [Authorize(Roles = Roles.Customer)]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            CreateTicketViewModel model = new CreateTicketViewModel();
            model.Categories = await _ticketService.GetCategoriesAsync();
            return View(model);
        }

        [Authorize(Roles = Roles.Customer)]
        [HttpPost]
        public async Task<IActionResult> Create(CreateTicketViewModel model)
        {
            if (ModelState.IsValid)
            {
                ServiceResult<Ticket> result = await _ticketService.CreateTicketAsync(
                    CurrentUser, model.Title, model.Description, model.CategoryId, model.Priority);

                if (result.Success)
                {
                    TempData["Success"] = "Ticket #" + result.Data.Id + " was created.";
                    return RedirectToAction("Details", new { id = result.Data.Id });
                }
                ModelState.AddModelError("", result.Message);
            }

            model.Categories = await _ticketService.GetCategoriesAsync();
            return View(model);
        }

        // ---------------- Actions on one ticket ----------------
        [HttpPost]
        public async Task<IActionResult> AddComment(int id, string body)
        {
            ServiceResult result = await _ticketService.AddCommentAsync(id, CurrentUser, body);
            IActionResult special = CheckSpecialError(result);
            if (special != null)
            {
                return special;
            }
            ShowResult(result, "Comment added.");
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, TicketStatus status)
        {
            ServiceResult result = await _ticketService.UpdateStatusAsync(id, CurrentUser, status);
            IActionResult special = CheckSpecialError(result);
            if (special != null)
            {
                return special;
            }
            ShowResult(result, "Status updated.");
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePriority(int id, TicketPriority priority)
        {
            ServiceResult result = await _ticketService.UpdatePriorityAsync(id, CurrentUser, priority);
            IActionResult special = CheckSpecialError(result);
            if (special != null)
            {
                return special;
            }
            ShowResult(result, "Priority updated.");
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        public async Task<IActionResult> SaveResolution(int id, string resolutionDetails)
        {
            ServiceResult result = await _ticketService.SaveResolutionAsync(id, CurrentUser, resolutionDetails);
            IActionResult special = CheckSpecialError(result);
            if (special != null)
            {
                return special;
            }
            ShowResult(result, "Resolution details saved.");
            return RedirectToAction("Details", new { id = id });
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Assign(int id, string agentUserId)
        {
            ServiceResult result = await _ticketService.AssignAgentAsync(id, CurrentUser, agentUserId);
            IActionResult special = CheckSpecialError(result);
            if (special != null)
            {
                return special;
            }
            ShowResult(result, "Ticket assigned.");
            return RedirectToAction("Details", new { id = id });
        }
    }
}
