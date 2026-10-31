using HelpDeskPro.Data;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Services.Tickets
{
    // All ticket business rules live here (NOT in the controllers).
    //
    // Access rules:
    //   Customer : sees and comments on his OWN tickets only
    //   Agent    : sees and manages tickets ASSIGNED to him only
    //   Admin    : sees everything, assigns agents, can manage every ticket
    public class TicketService : ITicketService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;

        public TicketService(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // ------------------------------------------------------------
        // Access helper methods
        // ------------------------------------------------------------

        private bool CanView(Ticket ticket, UserContext user)
        {
            if (user.IsAdmin)
            {
                return true;
            }
            if (user.IsAgent)
            {
                return ticket.AssignedAgentId == user.UserId;
            }
            return ticket.CreatedByUserId == user.UserId;
        }

        private bool CanManage(Ticket ticket, UserContext user)
        {
            if (user.IsAdmin)
            {
                return true;
            }
            if (user.IsAgent)
            {
                return ticket.AssignedAgentId == user.UserId;
            }
            return false;
        }

        private void AddHistory(Ticket ticket, string actorUserId, string fieldName, string oldValue, string newValue)
        {
            TicketHistory history = new TicketHistory();
            history.TicketId = ticket.Id;
            history.ActorUserId = actorUserId;
            history.FieldName = fieldName;
            history.OldValue = oldValue;
            history.NewValue = newValue;
            history.CreatedAt = DateTime.UtcNow;
            _db.TicketHistory.Add(history);
        }

        // Loads a ticket and checks that this user may manage it
        private async Task<ServiceResult<Ticket>> LoadManageableTicketAsync(int ticketId, UserContext user)
        {
            Ticket ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.NotFound, "Ticket not found.");
            }
            if (!CanView(ticket, user))
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Forbidden, "You do not have access to this ticket.");
            }
            if (!CanManage(ticket, user))
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Forbidden, "Only the assigned agent or an admin can change this ticket.");
            }
            return ServiceResult<Ticket>.Ok(ticket);
        }

        // ------------------------------------------------------------
        // Read
        // ------------------------------------------------------------

        public async Task<List<Category>> GetCategoriesAsync()
        {
            return await _db.Categories.OrderBy(c => c.Name).ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetStatusCountsAsync(UserContext user)
        {
            IQueryable<Ticket> query = _db.Tickets;
            if (user.IsCustomer)
            {
                query = query.Where(t => t.CreatedByUserId == user.UserId);
            }
            else if (user.IsAgent)
            {
                query = query.Where(t => t.AssignedAgentId == user.UserId);
            }

            Dictionary<string, int> counts = new Dictionary<string, int>();
            foreach (TicketStatus status in Enum.GetValues(typeof(TicketStatus)))
            {
                int count = await query.CountAsync(t => t.Status == status);
                counts.Add(status.ToString(), count);
            }
            return counts;
        }

        public async Task<PagedResult<Ticket>> GetTicketsAsync(UserContext user, TicketFilter filter)
        {
            IQueryable<Ticket> query = _db.Tickets
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedAgent);

            // 1) Role rule - applied inside the database query
            if (user.IsCustomer)
            {
                query = query.Where(t => t.CreatedByUserId == user.UserId);
            }
            else if (user.IsAgent)
            {
                query = query.Where(t => t.AssignedAgentId == user.UserId);
            }

            // 2) Optional filters
            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                string search = filter.Search.Trim().ToLower();
                query = query.Where(t => t.Title.ToLower().Contains(search) || t.Description.ToLower().Contains(search));
            }
            if (filter.Status.HasValue)
            {
                query = query.Where(t => t.Status == filter.Status.Value);
            }
            if (filter.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == filter.Priority.Value);
            }
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(t => t.CategoryId == filter.CategoryId.Value);
            }
            if (filter.OnlyUnassigned && user.IsAdmin)
            {
                query = query.Where(t => t.AssignedAgentId == null);
            }

            // 3) Sorting
            if (filter.SortBy == "oldest")
            {
                query = query.OrderBy(t => t.CreatedAt);
            }
            else if (filter.SortBy == "priority")
            {
                query = query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.CreatedAt);
            }
            else if (filter.SortBy == "status")
            {
                query = query.OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt);
            }
            else
            {
                query = query.OrderByDescending(t => t.CreatedAt);
            }

            // 4) Paging
            int pageSize = filter.PageSize;
            if (pageSize < 1 || pageSize > 50)
            {
                pageSize = 10;
            }
            int page = filter.Page;
            if (page < 1)
            {
                page = 1;
            }

            int totalCount = await query.CountAsync();
            List<Ticket> items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            PagedResult<Ticket> result = new PagedResult<Ticket>();
            result.Items = items;
            result.Page = page;
            result.PageSize = pageSize;
            result.TotalCount = totalCount;
            return result;
        }

        public async Task<ServiceResult<Ticket>> GetTicketAsync(int ticketId, UserContext user)
        {
            Ticket ticket = await _db.Tickets
                .Include(t => t.Category)
                .Include(t => t.CreatedByUser)
                .Include(t => t.AssignedAgent)
                .Include(t => t.Comments).ThenInclude(c => c.Author)
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket == null)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.NotFound, "Ticket not found.");
            }
            if (!CanView(ticket, user))
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Forbidden, "You do not have access to this ticket.");
            }

            ticket.Comments = ticket.Comments.OrderBy(c => c.CreatedAt).ToList();
            return ServiceResult<Ticket>.Ok(ticket);
        }

        public async Task<ServiceResult<List<TicketHistory>>> GetHistoryAsync(int ticketId, UserContext user)
        {
            Ticket ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null)
            {
                return ServiceResult<List<TicketHistory>>.Fail(ErrorType.NotFound, "Ticket not found.");
            }
            if (!CanView(ticket, user))
            {
                return ServiceResult<List<TicketHistory>>.Fail(ErrorType.Forbidden, "You do not have access to this ticket.");
            }

            List<TicketHistory> history = await _db.TicketHistory
                .Include(h => h.Actor)
                .Where(h => h.TicketId == ticketId)
                .OrderBy(h => h.CreatedAt)
                .ToListAsync();

            return ServiceResult<List<TicketHistory>>.Ok(history);
        }

        // ------------------------------------------------------------
        // Create
        // ------------------------------------------------------------

        public async Task<ServiceResult<Ticket>> CreateTicketAsync(UserContext user, string title, string description, int categoryId, TicketPriority priority)
        {
            if (!user.IsCustomer)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Forbidden, "Only customers can create tickets.");
            }
            if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Validation, "Title is required (maximum 200 characters).");
            }
            if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 4000)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Validation, "Description is required (maximum 4000 characters).");
            }
            if (!Enum.IsDefined(typeof(TicketPriority), priority))
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Validation, "Invalid priority.");
            }

            bool categoryExists = await _db.Categories.AnyAsync(c => c.Id == categoryId);
            if (!categoryExists)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Validation, "Please select a valid category.");
            }

            Ticket ticket = new Ticket();
            ticket.Title = title.Trim();
            ticket.Description = description.Trim();
            ticket.CategoryId = categoryId;
            ticket.Priority = priority;
            ticket.Status = TicketStatus.Open;
            ticket.CreatedByUserId = user.UserId;
            ticket.CreatedAt = DateTime.UtcNow;
            ticket.UpdatedAt = DateTime.UtcNow;

            _db.Tickets.Add(ticket);
            await _db.SaveChangesAsync();

            AddHistory(ticket, user.UserId, "Ticket", null, "Created");
            await _db.SaveChangesAsync();

            return ServiceResult<Ticket>.Ok(ticket);
        }

        // ------------------------------------------------------------
        // Update
        // ------------------------------------------------------------

        public async Task<ServiceResult> AddCommentAsync(int ticketId, UserContext user, string body)
        {
            if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 2000)
            {
                return ServiceResult.Fail(ErrorType.Validation, "Comment is required (maximum 2000 characters).");
            }

            Ticket ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null)
            {
                return ServiceResult.Fail(ErrorType.NotFound, "Ticket not found.");
            }
            if (!CanView(ticket, user))
            {
                return ServiceResult.Fail(ErrorType.Forbidden, "You do not have access to this ticket.");
            }
            if (ticket.Status == TicketStatus.Closed)
            {
                return ServiceResult.Fail(ErrorType.Conflict, "This ticket is closed. Comments are not allowed.");
            }

            TicketComment comment = new TicketComment();
            comment.TicketId = ticket.Id;
            comment.AuthorUserId = user.UserId;
            comment.Body = body.Trim();
            comment.CreatedAt = DateTime.UtcNow;
            _db.TicketComments.Add(comment);

            ticket.UpdatedAt = DateTime.UtcNow;
            AddHistory(ticket, user.UserId, "Comment", null, "Comment added");

            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> UpdateStatusAsync(int ticketId, UserContext user, TicketStatus newStatus)
        {
            if (!Enum.IsDefined(typeof(TicketStatus), newStatus))
            {
                return ServiceResult.Fail(ErrorType.Validation, "Invalid status.");
            }

            ServiceResult<Ticket> loaded = await LoadManageableTicketAsync(ticketId, user);
            if (!loaded.Success)
            {
                return ServiceResult.Fail(loaded.ErrorType, loaded.Message);
            }
            Ticket ticket = loaded.Data;

            if (ticket.Status == newStatus)
            {
                return ServiceResult.Fail(ErrorType.Validation, "The ticket already has this status.");
            }

            // Business rule 1: only an admin can change a closed ticket
            if (ticket.Status == TicketStatus.Closed && !user.IsAdmin)
            {
                return ServiceResult.Fail(ErrorType.Conflict, "Only an admin can re-open a closed ticket.");
            }

            // Business rule 2: resolution details are needed before Resolved / Closed
            bool needsResolution = newStatus == TicketStatus.Resolved || newStatus == TicketStatus.Closed;
            if (needsResolution && string.IsNullOrWhiteSpace(ticket.ResolutionDetails))
            {
                return ServiceResult.Fail(ErrorType.Validation, "Please write the resolution details before resolving or closing the ticket.");
            }

            string oldStatus = ticket.Status.ToString();
            ticket.Status = newStatus;
            ticket.UpdatedAt = DateTime.UtcNow;

            if (needsResolution)
            {
                if (!ticket.ResolvedAt.HasValue)
                {
                    ticket.ResolvedAt = DateTime.UtcNow;
                }
            }
            else
            {
                ticket.ResolvedAt = null;
            }

            AddHistory(ticket, user.UserId, "Status", oldStatus, newStatus.ToString());
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> UpdatePriorityAsync(int ticketId, UserContext user, TicketPriority newPriority)
        {
            if (!Enum.IsDefined(typeof(TicketPriority), newPriority))
            {
                return ServiceResult.Fail(ErrorType.Validation, "Invalid priority.");
            }

            ServiceResult<Ticket> loaded = await LoadManageableTicketAsync(ticketId, user);
            if (!loaded.Success)
            {
                return ServiceResult.Fail(loaded.ErrorType, loaded.Message);
            }
            Ticket ticket = loaded.Data;

            if (ticket.Priority == newPriority)
            {
                return ServiceResult.Fail(ErrorType.Validation, "The ticket already has this priority.");
            }

            string oldPriority = ticket.Priority.ToString();
            ticket.Priority = newPriority;
            ticket.UpdatedAt = DateTime.UtcNow;

            AddHistory(ticket, user.UserId, "Priority", oldPriority, newPriority.ToString());
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> SaveResolutionAsync(int ticketId, UserContext user, string resolutionDetails)
        {
            if (string.IsNullOrWhiteSpace(resolutionDetails) || resolutionDetails.Trim().Length > 4000)
            {
                return ServiceResult.Fail(ErrorType.Validation, "Resolution details are required (maximum 4000 characters).");
            }

            ServiceResult<Ticket> loaded = await LoadManageableTicketAsync(ticketId, user);
            if (!loaded.Success)
            {
                return ServiceResult.Fail(loaded.ErrorType, loaded.Message);
            }
            Ticket ticket = loaded.Data;

            ticket.ResolutionDetails = resolutionDetails.Trim();
            ticket.UpdatedAt = DateTime.UtcNow;

            AddHistory(ticket, user.UserId, "Resolution", null, "Resolution details saved");
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> AssignAgentAsync(int ticketId, UserContext user, string agentUserId)
        {
            if (!user.IsAdmin)
            {
                return ServiceResult.Fail(ErrorType.Forbidden, "Only an admin can assign tickets.");
            }

            Ticket ticket = await _db.Tickets.Include(t => t.AssignedAgent).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket == null)
            {
                return ServiceResult.Fail(ErrorType.NotFound, "Ticket not found.");
            }

            ApplicationUser agent = await _userManager.FindByIdAsync(agentUserId ?? "");
            if (agent == null)
            {
                return ServiceResult.Fail(ErrorType.Validation, "Agent not found.");
            }
            bool isAgent = await _userManager.IsInRoleAsync(agent, Roles.Agent);
            if (!isAgent)
            {
                return ServiceResult.Fail(ErrorType.Validation, "The selected user is not a support agent.");
            }
            if (ticket.AssignedAgentId == agent.Id)
            {
                return ServiceResult.Fail(ErrorType.Validation, "The ticket is already assigned to this agent.");
            }

            string oldAgent = "Unassigned";
            if (ticket.AssignedAgent != null)
            {
                oldAgent = ticket.AssignedAgent.Email;
            }

            ticket.AssignedAgentId = agent.Id;
            ticket.UpdatedAt = DateTime.UtcNow;

            // A new assignment moves an Open ticket to InProgress
            if (ticket.Status == TicketStatus.Open)
            {
                AddHistory(ticket, user.UserId, "Status", TicketStatus.Open.ToString(), TicketStatus.InProgress.ToString());
                ticket.Status = TicketStatus.InProgress;
            }

            AddHistory(ticket, user.UserId, "Assigned agent", oldAgent, agent.Email);
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }
    }
}
