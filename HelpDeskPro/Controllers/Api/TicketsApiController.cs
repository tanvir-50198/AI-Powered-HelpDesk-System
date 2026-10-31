using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers.Api
{
    [Route("api/tickets")]
    [Authorize]
    public class TicketsApiController : ApiControllerBase
    {
        private readonly ITicketService _ticketService;

        public TicketsApiController(ITicketService ticketService)
        {
            _ticketService = ticketService;
        }

        // POST /api/tickets  (customers)
        [HttpPost]
        [Authorize(Roles = Roles.Customer)]
        public async Task<ActionResult<TicketDto>> Create([FromBody] CreateTicketRequest request)
        {
            ServiceResult<Ticket> created = await _ticketService.CreateTicketAsync(
                CurrentUser, request.Title, request.Description, request.CategoryId, request.Priority);
            if (!created.Success)
            {
                return ErrorResult(created);
            }

            // load again so that category / user names are included
            ServiceResult<Ticket> loaded = await _ticketService.GetTicketAsync(created.Data.Id, CurrentUser);
            TicketDto dto = TicketMapper.ToDto(loaded.Data, true);
            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        // GET /api/tickets?search=&status=&priority=&categoryId=&sortBy=&page=&pageSize=
        [HttpGet]
        public async Task<ActionResult<PagedResult<TicketDto>>> GetAll([FromQuery] TicketFilter filter)
        {
            PagedResult<Ticket> tickets = await _ticketService.GetTicketsAsync(CurrentUser, filter);

            PagedResult<TicketDto> result = new PagedResult<TicketDto>();
            result.Page = tickets.Page;
            result.PageSize = tickets.PageSize;
            result.TotalCount = tickets.TotalCount;
            foreach (Ticket ticket in tickets.Items)
            {
                result.Items.Add(TicketMapper.ToDto(ticket, false));
            }
            return Ok(result);
        }

        // GET /api/tickets/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TicketDto>> GetById(int id)
        {
            ServiceResult<Ticket> result = await _ticketService.GetTicketAsync(id, CurrentUser);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return Ok(TicketMapper.ToDto(result.Data, true));
        }

        // PATCH /api/tickets/5/status  (assigned agent or admin)
        [HttpPatch("{id}/status")]
        public async Task<ActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            ServiceResult result = await _ticketService.UpdateStatusAsync(id, CurrentUser, request.Status);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return NoContent();
        }

        // POST /api/tickets/5/comments
        [HttpPost("{id}/comments")]
        public async Task<ActionResult> AddComment(int id, [FromBody] AddCommentRequest request)
        {
            ServiceResult result = await _ticketService.AddCommentAsync(id, CurrentUser, request.Body);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return StatusCode(201);
        }

        // GET /api/tickets/5/history
        [HttpGet("{id}/history")]
        public async Task<ActionResult<List<HistoryDto>>> GetHistory(int id)
        {
            ServiceResult<List<TicketHistory>> result = await _ticketService.GetHistoryAsync(id, CurrentUser);
            if (!result.Success)
            {
                return ErrorResult(result);
            }

            List<HistoryDto> list = new List<HistoryDto>();
            foreach (TicketHistory history in result.Data)
            {
                list.Add(TicketMapper.ToDto(history));
            }
            return Ok(list);
        }
    }
}
