using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using HelpDeskPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    [Authorize(Roles = Roles.Customer)]
    public class CustomerController : AppControllerBase
    {
        private readonly ITicketService _ticketService;

        public CustomerController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        public async Task<IActionResult> Dashboard()
        {
            UserContext user = CurrentUser;

            TicketFilter filter = new TicketFilter();
            filter.PageSize = 5;

            DashboardViewModel model = new DashboardViewModel();
            model.StatusCounts = await _ticketService.GetStatusCountsAsync(user);

            PagedResult<Ticket> recent = await _ticketService.GetTicketsAsync(user, filter);
            model.Tickets = recent.Items;

            return View(model);
        }
    }
}
