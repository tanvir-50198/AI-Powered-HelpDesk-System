using System.ComponentModel.DataAnnotations;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;

namespace HelpDeskPro.ViewModels
{
    public class CreateTicketViewModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(4000)]
        public string Description { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;

        public List<Category> Categories { get; set; } = new List<Category>();
    }

    public class TicketListViewModel
    {
        public PagedResult<Ticket> Result { get; set; }
        public TicketFilter Filter { get; set; }
        public List<Category> Categories { get; set; } = new List<Category>();
        public bool IsAdmin { get; set; }
    }

    public class TicketDetailsViewModel
    {
        public Ticket Ticket { get; set; }
        public List<TicketHistory> History { get; set; } = new List<TicketHistory>();

        // Agents that an admin can choose from
        public List<ApplicationUser> Agents { get; set; } = new List<ApplicationUser>();

        public bool CanManage { get; set; }
        public bool IsAdmin { get; set; }
        public bool CanComment { get; set; }
    }

    // Used by the three dashboards
    public class DashboardViewModel
    {
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
        public List<Ticket> Tickets { get; set; } = new List<Ticket>();
        public int TotalUsers { get; set; }
        public int UnassignedTickets { get; set; }
        public int ActiveConversations { get; set; }

        public int TotalTickets
        {
            get
            {
                int total = 0;
                foreach (int count in StatusCounts.Values)
                {
                    total += count;
                }
                return total;
            }
        }
    }
}
