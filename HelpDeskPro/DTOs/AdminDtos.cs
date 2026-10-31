using HelpDeskPro.Models;

namespace HelpDeskPro.DTOs
{
    public class DocumentDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Category { get; set; }
        public string SourceType { get; set; }
        public DocumentAudience Audience { get; set; }
        public ApprovalStatus ApprovalStatus { get; set; }
        public ProcessingStatus ProcessingStatus { get; set; }
        public string ProcessingError { get; set; }
        public int Version { get; set; }
        public DateTime UpdatedAt { get; set; }

        public static DocumentDto FromEntity(KnowledgeDocument document)
        {
            DocumentDto dto = new DocumentDto();
            dto.Id = document.Id;
            dto.Title = document.Title;
            dto.Category = document.Category;
            dto.SourceType = document.SourceType;
            dto.Audience = document.Audience;
            dto.ApprovalStatus = document.ApprovalStatus;
            dto.ProcessingStatus = document.ProcessingStatus;
            dto.ProcessingError = document.ProcessingError;
            dto.Version = document.Version;
            dto.UpdatedAt = document.UpdatedAt;
            return dto;
        }
    }

    public class StatusCountDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class DailyCountDto
    {
        public string Day { get; set; }
        public int Created { get; set; }
        public int Resolved { get; set; }
    }

    public class UnansweredQuestionDto
    {
        public int ConversationId { get; set; }
        public string Question { get; set; }
        public DateTime AskedAt { get; set; }
    }

    // Everything shown on the admin report page and returned by /api/admin/reports/summary
    public class AdminSummaryDto
    {
        public int TotalTickets { get; set; }
        public List<StatusCountDto> StatusDistribution { get; set; } = new List<StatusCountDto>();
        public List<DailyCountDto> Last7Days { get; set; } = new List<DailyCountDto>();

        // The SLA hours are a DEMO value from appsettings.json (not a real company promise)
        public int DemoSlaHours { get; set; }
        public int OverdueTickets { get; set; }

        public int TotalConversations { get; set; }
        public int EscalatedConversations { get; set; }
        public double EscalationRatePercent { get; set; }

        public int HelpfulFeedback { get; set; }
        public int NotHelpfulFeedback { get; set; }
        public double HelpfulRatePercent { get; set; }

        public int FallbackAnswers { get; set; }
        public List<UnansweredQuestionDto> UnansweredQuestions { get; set; } = new List<UnansweredQuestionDto>();

        public int ApprovedDocuments { get; set; }
        public int FailedDocuments { get; set; }
    }
}
