using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using HelpDeskPro.Services.Tickets;
using HelpDeskPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Controllers
{
    [Authorize(Roles = Roles.Admin)]
    public class AdminController : AppControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ITicketService ticketService,
            IReportService reportService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext db,
            ILogger<AdminController> logger)
        {
            _ticketService = ticketService;
            _reportService = reportService;
            _userManager = userManager;
            _db = db;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            UserContext user = CurrentUser;

            DashboardViewModel model = new DashboardViewModel();
            model.StatusCounts = await _ticketService.GetStatusCountsAsync(user);
            model.TotalUsers = await _userManager.Users.CountAsync();
            model.UnassignedTickets = await _db.Tickets.CountAsync(t => t.AssignedAgentId == null && t.Status != TicketStatus.Closed);
            model.ActiveConversations = await _db.ChatConversations.CountAsync();

            TicketFilter filter = new TicketFilter();
            filter.OnlyUnassigned = true;
            filter.PageSize = 5;
            PagedResult<Ticket> unassigned = await _ticketService.GetTicketsAsync(user, filter);
            model.Tickets = unassigned.Items;

            return View(model);
        }

        public async Task<IActionResult> Reports()
        {
            AdminSummaryDto summary = await _reportService.GetSummaryAsync();
            return View(summary);
        }

        // ---------------- User management ----------------
        public async Task<IActionResult> Users()
        {
            List<ApplicationUser> users = await _userManager.Users.OrderBy(u => u.Email).ToListAsync();

            List<UserListItemViewModel> model = new List<UserListItemViewModel>();
            foreach (ApplicationUser user in users)
            {
                IList<string> roles = await _userManager.GetRolesAsync(user);

                UserListItemViewModel item = new UserListItemViewModel();
                item.Id = user.Id;
                item.Email = user.Email;
                item.FullName = user.FullName;
                item.CreatedAt = user.CreatedAt;
                item.Role = roles.Count > 0 ? roles[0] : Roles.Customer;
                model.Add(item);
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(string userId, string role)
        {
            if (role != Roles.Customer && role != Roles.Agent && role != Roles.Admin)
            {
                TempData["Error"] = "Invalid role.";
                return RedirectToAction("Users");
            }

            ApplicationUser user = await _userManager.FindByIdAsync(userId ?? "");
            if (user == null)
            {
                return NotFound();
            }

            // An admin cannot remove his own admin rights by accident
            if (user.Id == CurrentUser.UserId)
            {
                TempData["Error"] = "You cannot change your own role.";
                return RedirectToAction("Users");
            }

            IList<string> oldRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, oldRoles);
            await _userManager.AddToRoleAsync(user, role);

            _logger.LogInformation("AUDIT: admin {AdminId} changed role of user {UserId} to {Role}", CurrentUser.UserId, user.Id, role);
            TempData["Success"] = user.Email + " is now " + role + ".";
            return RedirectToAction("Users");
        }
    }
}
