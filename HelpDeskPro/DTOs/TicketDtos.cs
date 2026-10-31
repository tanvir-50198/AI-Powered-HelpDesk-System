using System.ComponentModel.DataAnnotations;
using HelpDeskPro.Models;

namespace HelpDeskPro.DTOs
{
    // ---------- Requests (data sent by the API client) ----------
    public class CreateTicketRequest
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(4000)]
        public string Description { get; set; }

        public int CategoryId { get; set; }

        public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    }

    public class UpdateStatusRequest
    {
        public TicketStatus Status { get; set; }
    }

    public class AddCommentRequest
    {
        [Required]
        [StringLength(2000)]
        public string Body { get; set; }
    }

    // ---------- Responses (data returned by the API) ----------
    public class TicketDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }
        public string CreatedBy { get; set; }
        public string AssignedAgent { get; set; }
        public string ResolutionDetails { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<CommentDto> Comments { get; set; } = new List<CommentDto>();
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class HistoryDto
    {
        public string Actor { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Converts database entities to the small objects returned by the API
    public static class TicketMapper
    {
        public static TicketDto ToDto(Ticket ticket, bool includeComments)
        {
            TicketDto dto = new TicketDto();
            dto.Id = ticket.Id;
            dto.Title = ticket.Title;
            dto.Description = ticket.Description;
            dto.Status = ticket.Status;
            dto.Priority = ticket.Priority;
            dto.ResolutionDetails = ticket.ResolutionDetails;
            dto.CreatedAt = ticket.CreatedAt;
            dto.UpdatedAt = ticket.UpdatedAt;

            if (ticket.Category != null)
            {
                dto.Category = ticket.Category.Name;
            }
            if (ticket.CreatedByUser != null)
            {
                dto.CreatedBy = ticket.CreatedByUser.Email;
            }
            if (ticket.AssignedAgent != null)
            {
                dto.AssignedAgent = ticket.AssignedAgent.Email;
            }

            if (includeComments)
            {
                foreach (TicketComment comment in ticket.Comments)
                {
                    CommentDto commentDto = new CommentDto();
                    commentDto.Id = comment.Id;
                    commentDto.Body = comment.Body;
                    commentDto.CreatedAt = comment.CreatedAt;
                    if (comment.Author != null)
                    {
                        commentDto.Author = comment.Author.Email;
                    }
                    dto.Comments.Add(commentDto);
                }
            }

            return dto;
        }

        public static HistoryDto ToDto(TicketHistory history)
        {
            HistoryDto dto = new HistoryDto();
            dto.FieldName = history.FieldName;
            dto.OldValue = history.OldValue;
            dto.NewValue = history.NewValue;
            dto.CreatedAt = history.CreatedAt;
            if (history.Actor != null)
            {
                dto.Actor = history.Actor.Email;
            }
            return dto;
        }
    }
}
