using HelpDeskPro.DTOs;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers.Api
{
    // The ticket actions that existed in the MVC pages but not yet in the REST API.
    // All rules (who may do what, "resolution required", ...) stay in TicketService,
    // exactly like the MVC controllers - this class only translates HTTP <-> service.
    [Route("api/tickets")]
    [Authorize]
    public class TicketsManagementApiController : ApiControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsManagementApiController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        // PATCH /api/tickets/5/priority  (assigned agent or admin)
        [HttpPatch("{id}/priority")]
        public async Task<ActionResult> UpdatePriority(int id, [FromBody] UpdatePriorityRequest request)
        {
            ServiceResult result = await _ticketService.UpdatePriorityAsync(id, CurrentUser, request.Priority);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return NoContent();
        }

        // PUT /api/tickets/5/resolution  (assigned agent or admin)
        [HttpPut("{id}/resolution")]
        public async Task<ActionResult> SaveResolution(int id, [FromBody] SaveResolutionRequest request)
        {
            ServiceResult result = await _ticketService.SaveResolutionAsync(id, CurrentUser, request.ResolutionDetails);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return NoContent();
        }

        // POST /api/tickets/5/assign  (admin)
        [HttpPost("{id}/assign")]
        public async Task<ActionResult> Assign(int id, [FromBody] AssignAgentRequest request)
        {
            ServiceResult result = await _ticketService.AssignAgentAsync(id, CurrentUser, request.AgentUserId);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return NoContent();
        }
    }
}
