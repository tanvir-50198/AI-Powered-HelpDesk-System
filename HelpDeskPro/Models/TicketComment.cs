using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    public class TicketComment
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public string AuthorUserId { get; set; }
        public ApplicationUser Author { get; set; }

        [Required]
        [StringLength(2000)]
        public string Body { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
