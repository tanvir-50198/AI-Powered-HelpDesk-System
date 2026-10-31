using System.ComponentModel.DataAnnotations;
using HelpDeskPro.Models;

namespace HelpDeskPro.ViewModels
{
    public class ChatPageViewModel
    {
        public List<ChatConversation> Conversations { get; set; } = new List<ChatConversation>();

        // null = a new, empty chat
        public ChatConversation Selected { get; set; }
    }

    public class CreateTicketFromChatViewModel
    {
        public int ConversationId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(4000)]
        [Display(Name = "Summary (please check and edit)")]
        public string Description { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        [Display(Name = "I checked the summary and want to create the ticket")]
        public bool Confirmed { get; set; }

        public List<Category> Categories { get; set; } = new List<Category>();
    }
}
