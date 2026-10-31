using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    // One row for every important change on a ticket (audit trail)
    public class TicketHistory
    {
        public int Id { get; set; }

        public int TicketId { get; set; }
        public Ticket Ticket { get; set; }

        public string ActorUserId { get; set; }
        public ApplicationUser Actor { get; set; }

        [StringLength(100)]
        public string FieldName { get; set; }

        [StringLength(500)]
        public string OldValue { get; set; }

        [StringLength(500)]
        public string NewValue { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
