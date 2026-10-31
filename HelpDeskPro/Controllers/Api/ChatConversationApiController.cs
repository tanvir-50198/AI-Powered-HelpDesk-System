using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Controllers.Api
{
    // Returns ONE conversation with all its messages, sources and the customer's ratings,
    // so the React chat can open an old conversation (ChatApiController only lists them).
    [Route("api/chat")]
    [Authorize(Roles = Roles.Customer)]
    public class ChatConversationApiController : ApiControllerBase
    {
        private readonly IRAGChatService _chatService;
        private readonly ApplicationDbContext _db;

        public ChatConversationApiController(IRAGChatService chatService, ApplicationDbContext db)
        {
            _chatService = chatService;
            _db = db;
        }

        // GET /api/chat/conversations/5
        [HttpGet("conversations/{id}")]
        public async Task<ActionResult<ConversationDetailDto>> GetConversation(int id)
        {
            // checks that the conversation exists AND belongs to the caller (404 / 403 otherwise)
            ServiceResult<ChatConversation> result = await _chatService.GetConversationAsync(id, CurrentUser.UserId);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            ChatConversation conversation = result.Data;

            // the customer's own ratings for these messages (oldest first, so the newest rating wins)
            List<int> messageIds = conversation.Messages.Select(m => m.Id).ToList();
            List<ChatFeedback> ratings = await _db.ChatFeedback
                .Where(f => f.CustomerUserId == CurrentUser.UserId && messageIds.Contains(f.MessageId))
                .OrderBy(f => f.CreatedAt).ThenBy(f => f.Id)
                .ToListAsync();

            Dictionary<int, bool> helpfulByMessage = new Dictionary<int, bool>();
            foreach (ChatFeedback rating in ratings)
            {
                helpfulByMessage[rating.MessageId] = rating.IsHelpful;
            }

            ConversationDetailDto dto = new ConversationDetailDto();
            dto.Id = conversation.Id;
            dto.Title = conversation.Title;
            dto.UpdatedAt = conversation.UpdatedAt;
            dto.LinkedTicketId = conversation.LinkedTicketId;

            foreach (ChatMessage message in conversation.Messages)
            {
                ConversationMessageDto item = new ConversationMessageDto();
                item.Id = message.Id;
                item.Role = message.Role;
                item.Content = message.Content;
                item.IsFallback = message.IsFallback;
                item.CreatedAt = message.CreatedAt;
                if (helpfulByMessage.ContainsKey(message.Id))
                {
                    item.Helpful = helpfulByMessage[message.Id];
                }

                // sources in the order they were saved = the order of the [1] [2] markers in the answer
                foreach (ChatSource source in message.Sources.OrderBy(s => s.Id))
                {
                    SourceDto sourceDto = new SourceDto();
                    sourceDto.DocumentId = source.DocumentId;
                    sourceDto.Title = source.DisplayTitle;
                    sourceDto.Section = source.SectionLabel;
                    item.Sources.Add(sourceDto);
                }
                dto.Messages.Add(item);
            }
            return Ok(dto);
        }
    }
}
