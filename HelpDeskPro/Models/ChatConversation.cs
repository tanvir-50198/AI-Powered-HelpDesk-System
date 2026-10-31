using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    public class ChatConversation
    {
        public int Id { get; set; }

        public string CustomerUserId { get; set; }
        public ApplicationUser CustomerUser { get; set; }

        [StringLength(200)]
        public string Title { get; set; } = "New conversation";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Set when the customer escalated this chat into a support ticket
        public int? LinkedTicketId { get; set; }

        public List<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    }
}
