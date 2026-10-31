using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HelpDeskPro.Controllers.Api
{
    [Route("api/chat")]
    [Authorize(Roles = Roles.Customer)]
    public class ChatApiController : ApiControllerBase
    {
        private readonly IRAGChatService _chatService;

        public ChatApiController(IRAGChatService chatService)
        {
            _chatService = chatService;
        }

        // POST /api/chat/conversations
        [HttpPost("conversations")]
        public async Task<ActionResult<ConversationDto>> StartConversation()
        {
            ChatConversation conversation = await _chatService.CreateConversationAsync(CurrentUser.UserId);
            return StatusCode(201, ToDto(conversation));
        }

        // GET /api/chat/conversations
        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationDto>>> GetConversations()
        {
            List<ChatConversation> conversations = await _chatService.GetConversationsAsync(CurrentUser.UserId);

            List<ConversationDto> list = new List<ConversationDto>();
            foreach (ChatConversation conversation in conversations)
            {
                list.Add(ToDto(conversation));
            }
            return Ok(list);
        }

        // POST /api/chat/conversations/5/messages   (limited to 20 per minute)
        [HttpPost("conversations/{id}/messages")]
        [EnableRateLimiting("chat")]
        public async Task<ActionResult<ChatAnswerDto>> SendMessage(int id, [FromBody] SendMessageRequest request)
        {
            ServiceResult<ChatAnswerDto> result = await _chatService.AskAsync(id, CurrentUser.UserId, request.Question);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return Ok(result.Data);
        }

        // POST /api/chat/messages/12/feedback
        [HttpPost("messages/{id}/feedback")]
        public async Task<ActionResult> SendFeedback(int id, [FromBody] FeedbackRequest request)
        {
            ServiceResult result = await _chatService.SaveFeedbackAsync(id, CurrentUser.UserId, request.IsHelpful, request.Comment);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            return NoContent();
        }

        // POST /api/chat/conversations/5/create-ticket
        [HttpPost("conversations/{id}/create-ticket")]
        public async Task<ActionResult<TicketDto>> CreateTicket(int id, [FromBody] CreateTicketFromChatRequest request)
        {
            ServiceResult<Ticket> result = await _chatService.CreateTicketFromConversationAsync(id, CurrentUser.UserId, request);
            if (!result.Success)
            {
                return ErrorResult(result);
            }

            TicketDto dto = TicketMapper.ToDto(result.Data, false);
            return Created("/api/tickets/" + dto.Id, dto);
        }

        private ConversationDto ToDto(ChatConversation conversation)
        {
            ConversationDto dto = new ConversationDto();
            dto.Id = conversation.Id;
            dto.Title = conversation.Title;
            dto.UpdatedAt = conversation.UpdatedAt;
            dto.LinkedTicketId = conversation.LinkedTicketId;
            return dto;
        }
    }
}
