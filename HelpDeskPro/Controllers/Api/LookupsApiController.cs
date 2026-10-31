using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Controllers.Api
{
    // Small read-only endpoints that the React screens need.
    [Route("api")]
    [Authorize]
    public class LookupsApiController : ApiControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public LookupsApiController(ITicketService ticketService, ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _ticketService = ticketService;
            _db = db;
            _userManager = userManager;
        }

        // GET /api/lookups/categories
        [HttpGet("lookups/categories")]
        public async Task<ActionResult<List<CategoryDto>>> GetCategories()
        {
            List<Category> categories = await _ticketService.GetCategoriesAsync();

            List<CategoryDto> list = new List<CategoryDto>();
            foreach (Category category in categories)
            {
                CategoryDto dto = new CategoryDto();
                dto.Id = category.Id;
                dto.Name = category.Name;
                dto.Description = category.Description;
                list.Add(dto);
            }
            return Ok(list);
        }

        // GET /api/dashboard/summary
        // Everybody gets the ticket counts they are allowed to see. Admins get a few extra numbers.
        [HttpGet("dashboard/summary")]
        public async Task<ActionResult<DashboardSummaryDto>> GetDashboardSummary()
        {
            DashboardSummaryDto dto = new DashboardSummaryDto();
            dto.StatusCounts = await _ticketService.GetStatusCountsAsync(CurrentUser);

            if (CurrentUser.IsAdmin)
            {
                dto.UnassignedTickets = await _db.Tickets.CountAsync(t => t.AssignedAgentId == null);
                dto.TotalUsers = await _userManager.Users.CountAsync();
                dto.TotalConversations = await _db.ChatConversations.CountAsync();
            }
            return Ok(dto);
        }
    }
}
