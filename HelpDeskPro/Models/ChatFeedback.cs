using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    public class ChatFeedback
    {
        public int Id { get; set; }

        public int MessageId { get; set; }
        public ChatMessage Message { get; set; }

        public string CustomerUserId { get; set; }
        public ApplicationUser CustomerUser { get; set; }

        // true = "Helpful", false = "Not helpful"
        public bool IsHelpful { get; set; }

        [StringLength(500)]
        public string Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
