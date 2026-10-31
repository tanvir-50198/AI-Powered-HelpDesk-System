using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    public class ChatMessage
    {
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public ChatConversation Conversation { get; set; }

        // "User" or "Assistant"
        [StringLength(20)]
        public string Role { get; set; }

        public string Content { get; set; }

        // true = the assistant could not find enough evidence
        public bool IsFallback { get; set; }

        [StringLength(200)]
        public string ModelInfo { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<ChatSource> Sources { get; set; } = new List<ChatSource>();
        public List<ChatFeedback> Feedback { get; set; } = new List<ChatFeedback>();
    }
}
