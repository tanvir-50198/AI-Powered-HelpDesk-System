using System.ComponentModel.DataAnnotations;
using HelpDeskPro.Models;

namespace HelpDeskPro.DTOs
{
    public class SendMessageRequest
    {
        [Required]
        [StringLength(1000)]
        public string Question { get; set; }
    }

    public class FeedbackRequest
    {
        public bool IsHelpful { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }
    }

    public class CreateTicketFromChatRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        // The summary the customer has reviewed
        [Required]
        [StringLength(4000)]
        public string Description { get; set; }

        public int CategoryId { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        // The customer must confirm the summary
        public bool Confirmed { get; set; }
    }

    public class SourceDto
    {
        public int DocumentId { get; set; }
        public string Title { get; set; }
        public string Section { get; set; }
    }

    public class ChatAnswerDto
    {
        public int ConversationId { get; set; }
        public int MessageId { get; set; }
        public string Answer { get; set; }

        // true = no good evidence was found
        public bool IsFallback { get; set; }

        // true = the UI should offer "Create a support ticket"
        public bool CanCreateTicket { get; set; }

        public List<SourceDto> Sources { get; set; } = new List<SourceDto>();
    }

    public class ConversationDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? LinkedTicketId { get; set; }
    }
}
