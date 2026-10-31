using System.Text;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using HelpDeskPro.Services.Tickets;
using HelpDeskPro.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers
{
    // The chat PAGE. Sending questions is done by JavaScript calling /api/chat/...
    [Authorize(Roles = Roles.Customer)]
    public class ChatController : AppControllerBase
    {
        private readonly IRAGChatService _chatService;
        private readonly ITicketService _ticketService;

        public ChatController(IRAGChatService chatService, ITicketService ticketService)
        {
            _chatService = chatService;
            _ticketService = ticketService;
        }

        public async Task<IActionResult> Index(int? id)
        {
            ChatPageViewModel model = new ChatPageViewModel();
            model.Conversations = await _chatService.GetConversationsAsync(CurrentUser.UserId);

            if (id.HasValue)
            {
                ServiceResult<ChatConversation> result = await _chatService.GetConversationAsync(id.Value, CurrentUser.UserId);
                IActionResult special = CheckSpecialError(result);
                if (special != null)
                {
                    return special;
                }
                model.Selected = result.Data;
            }

            return View(model);
        }

        // Step 1 of escalation: show a summary that the customer can check and edit
        [HttpGet]
        public async Task<IActionResult> CreateTicket(int id)
        {
            ServiceResult<ChatConversation> result = await _chatService.GetConversationAsync(id, CurrentUser.UserId);
            IActionResult special = CheckSpecialError(result);
            if (special != null)
            {
                return special;
            }

            ChatConversation conversation = result.Data;
            if (conversation.LinkedTicketId.HasValue)
            {
                TempData["Error"] = "A ticket was already created from this conversation.";
                return RedirectToAction("Details", "Tickets", new { id = conversation.LinkedTicketId.Value });
            }

            CreateTicketFromChatViewModel model = new CreateTicketFromChatViewModel();
            model.ConversationId = id;
            model.Description = BuildSummary(conversation);
            model.Title = BuildTitle(conversation);
            model.Categories = await _ticketService.GetCategoriesAsync();
            return View(model);
        }

        // Step 2: the customer confirmed - create the ticket
        [HttpPost]
        public async Task<IActionResult> CreateTicket(CreateTicketFromChatViewModel model)
        {
            if (ModelState.IsValid)
            {
                HelpDeskPro.DTOs.CreateTicketFromChatRequest request = new HelpDeskPro.DTOs.CreateTicketFromChatRequest();
                request.Title = model.Title;
                request.Description = model.Description;
                request.CategoryId = model.CategoryId;
                request.Priority = model.Priority;
                request.Confirmed = model.Confirmed;

                ServiceResult<Ticket> result = await _chatService.CreateTicketFromConversationAsync(model.ConversationId, CurrentUser.UserId, request);
                IActionResult special = CheckSpecialError(result);
                if (special != null)
                {
                    return special;
                }

                if (result.Success)
                {
                    TempData["Success"] = "Ticket #" + result.Data.Id + " was created. An agent will help you soon.";
                    return RedirectToAction("Details", "Tickets", new { id = result.Data.Id });
                }
                ModelState.AddModelError("", result.Message);
            }

            model.Categories = await _ticketService.GetCategoriesAsync();
            return View(model);
        }

        // ---------------- helper methods ----------------
        private string BuildTitle(ChatConversation conversation)
        {
            foreach (ChatMessage message in conversation.Messages)
            {
                if (message.Role == "User")
                {
                    string text = message.Content;
                    if (text.Length > 100)
                    {
                        text = text.Substring(0, 100) + "...";
                    }
                    return text;
                }
            }
            return "Help needed";
        }

        private string BuildSummary(ChatConversation conversation)
        {
            List<ChatMessage> questions = conversation.Messages.Where(m => m.Role == "User").ToList();

            StringBuilder summary = new StringBuilder();
            summary.AppendLine("I asked the AI assistant and could not solve my problem.");
            summary.AppendLine();
            summary.AppendLine("My questions:");

            int start = questions.Count > 3 ? questions.Count - 3 : 0;
            for (int i = start; i < questions.Count; i++)
            {
                summary.AppendLine("- " + questions[i].Content);
            }

            summary.AppendLine();
            summary.AppendLine("Please help me.");
            return summary.ToString();
        }
    }
}
