using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;

namespace HelpDeskPro.Services.Tickets
{
    public interface ITicketService
    {
        Task<List<Category>> GetCategoriesAsync();

        // Number of tickets per status that this user is allowed to see
        Task<Dictionary<string, int>> GetStatusCountsAsync(UserContext user);

        Task<PagedResult<Ticket>> GetTicketsAsync(UserContext user, TicketFilter filter);

        Task<ServiceResult<Ticket>> GetTicketAsync(int ticketId, UserContext user);

        Task<ServiceResult<Ticket>> CreateTicketAsync(UserContext user, string title, string description, int categoryId, TicketPriority priority);

        Task<ServiceResult> AddCommentAsync(int ticketId, UserContext user, string body);

        Task<ServiceResult> UpdateStatusAsync(int ticketId, UserContext user, TicketStatus newStatus);

        Task<ServiceResult> UpdatePriorityAsync(int ticketId, UserContext user, TicketPriority newPriority);

        Task<ServiceResult> SaveResolutionAsync(int ticketId, UserContext user, string resolutionDetails);

        Task<ServiceResult> AssignAgentAsync(int ticketId, UserContext user, string agentUserId);

        Task<ServiceResult<List<TicketHistory>>> GetHistoryAsync(int ticketId, UserContext user);
    }
}
