using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(4000)]
        public string Description { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Open;
        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        // The customer who created the ticket
        public string CreatedByUserId { get; set; }
        public ApplicationUser CreatedByUser { get; set; }

        // The agent who works on the ticket (null = not assigned yet)
        public string AssignedAgentId { get; set; }
        public ApplicationUser AssignedAgent { get; set; }

        [StringLength(4000)]
        public string ResolutionDetails { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public List<TicketComment> Comments { get; set; } = new List<TicketComment>();
        public List<TicketHistory> History { get; set; } = new List<TicketHistory>();
    }
}
