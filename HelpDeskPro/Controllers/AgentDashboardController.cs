using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using HelpDeskPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    [Authorize(Roles = Roles.Agent)]
    public class AgentDashboardController : AppControllerBase
    {
        private readonly ITicketService _ticketService;

        public AgentDashboardController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        public async Task<IActionResult> Index()
        {
            UserContext user = CurrentUser;

            // The most urgent assigned tickets first
            TicketFilter filter = new TicketFilter();
            filter.SortBy = "priority";
            filter.PageSize = 10;

            DashboardViewModel model = new DashboardViewModel();
            model.StatusCounts = await _ticketService.GetStatusCountsAsync(user);

            PagedResult<Ticket> assigned = await _ticketService.GetTicketsAsync(user, filter);
            model.Tickets = assigned.Items;

            return View(model);
        }
    }
}
