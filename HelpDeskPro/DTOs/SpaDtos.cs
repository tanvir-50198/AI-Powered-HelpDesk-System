using System.ComponentModel.DataAnnotations;
using HelpDeskPro.Models;

namespace HelpDeskPro.DTOs
{
    // DTOs used only by the React frontend (see Controllers/Api/*Spa*.cs and friends).

    // ---------- Auth ----------
    public class LoginApiRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class RegisterApiRequest
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "The passwords do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class CurrentUserDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
    }

    // ---------- Lookups / dashboard ----------
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class DashboardSummaryDto
    {
        // key = status name ("Open", "InProgress", "Resolved", "Closed")
        public Dictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();

        // The three values below are only filled for admins (null for everybody else)
        public int? UnassignedTickets { get; set; }
        public int? TotalUsers { get; set; }
        public int? TotalConversations { get; set; }
    }

    // ---------- Ticket management ----------
    public class UpdatePriorityRequest
    {
        public TicketPriority Priority { get; set; }
    }

    public class SaveResolutionRequest
    {
        [Required]
        [StringLength(4000)]
        public string ResolutionDetails { get; set; }
    }

    public class AssignAgentRequest
    {
        [Required]
        public string AgentUserId { get; set; }
    }

    // ---------- Users ----------
    public class AdminUserDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AgentOptionDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
    }

    public class ChangeRoleRequest
    {
        [Required]
        public string Role { get; set; }
    }

    // ---------- Chat ----------
    public class ConversationMessageDto
    {
        public int Id { get; set; }

        // "User" or "Assistant"
        public string Role { get; set; }
        public string Content { get; set; }
        public bool IsFallback { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SourceDto> Sources { get; set; } = new List<SourceDto>();

        // true / false = the customer rated this answer, null = not rated yet
        public bool? Helpful { get; set; }
    }

    public class ConversationDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? LinkedTicketId { get; set; }
        public List<ConversationMessageDto> Messages { get; set; } = new List<ConversationMessageDto>();
    }

    public class ImportFolderResultDto
    {
        public int Imported { get; set; }
    }
}
