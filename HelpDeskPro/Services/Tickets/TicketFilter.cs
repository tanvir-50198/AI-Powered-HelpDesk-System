using HelpDeskPro.Models;

namespace HelpDeskPro.Services.Tickets
{
    // Search / filter / sort / paging options for the ticket list
    public class TicketFilter
    {
        public string Search { get; set; }
        public TicketStatus? Status { get; set; }
        public TicketPriority? Priority { get; set; }
        public int? CategoryId { get; set; }

        // Admin only: show tickets that have no agent yet
        public bool OnlyUnassigned { get; set; }

        // newest (default), oldest, priority, status
        public string SortBy { get; set; } = "newest";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
