using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Services.RAG
{
    public interface IReportService
    {
        Task<AdminSummaryDto> GetSummaryAsync();
    }

    // Builds the numbers for the admin dashboard.
    // Every number comes from real database queries - nothing is hard-coded.
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _db;
        private readonly IConfiguration _configuration;

        public ReportService(ApplicationDbContext db, IConfiguration configuration)
        {
            _db = db;
            _configuration = configuration;
        }

        public async Task<AdminSummaryDto> GetSummaryAsync()
        {
            AdminSummaryDto summary = new AdminSummaryDto();

            // ---- Tickets by status ----
            summary.TotalTickets = await _db.Tickets.CountAsync();
            foreach (TicketStatus status in Enum.GetValues(typeof(TicketStatus)))
            {
                StatusCountDto item = new StatusCountDto();
                item.Status = status.ToString();
                item.Count = await _db.Tickets.CountAsync(t => t.Status == status);
                summary.StatusDistribution.Add(item);
            }

            // ---- Created vs resolved (last 7 days) ----
            DateTime today = DateTime.UtcNow.Date;
            for (int i = 6; i >= 0; i--)
            {
                DateTime dayStart = today.AddDays(-i);
                DateTime dayEnd = dayStart.AddDays(1);

                DailyCountDto day = new DailyCountDto();
                day.Day = dayStart.ToString("dd MMM");
                day.Created = await _db.Tickets.CountAsync(t => t.CreatedAt >= dayStart && t.CreatedAt < dayEnd);
                day.Resolved = await _db.Tickets.CountAsync(t => t.ResolvedAt != null && t.ResolvedAt >= dayStart && t.ResolvedAt < dayEnd);
                summary.Last7Days.Add(day);
            }

            // ---- Overdue tickets (DEMO SLA from appsettings.json) ----
            int slaHours = 48;
            string slaSetting = _configuration["Sla:DemoHours"];
            if (!string.IsNullOrWhiteSpace(slaSetting))
            {
                int.TryParse(slaSetting, out slaHours);
            }
            summary.DemoSlaHours = slaHours;
            DateTime overdueLimit = DateTime.UtcNow.AddHours(-slaHours);
            summary.OverdueTickets = await _db.Tickets.CountAsync(t =>
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress) && t.CreatedAt < overdueLimit);

            // ---- Chat: escalation rate ----
            summary.TotalConversations = await _db.ChatConversations.CountAsync();
            summary.EscalatedConversations = await _db.ChatConversations.CountAsync(c => c.LinkedTicketId != null);
            if (summary.TotalConversations > 0)
            {
                summary.EscalationRatePercent = Math.Round(100.0 * summary.EscalatedConversations / summary.TotalConversations, 1);
            }

            // ---- Chat: feedback ----
            summary.HelpfulFeedback = await _db.ChatFeedback.CountAsync(f => f.IsHelpful);
            summary.NotHelpfulFeedback = await _db.ChatFeedback.CountAsync(f => !f.IsHelpful);
            int totalFeedback = summary.HelpfulFeedback + summary.NotHelpfulFeedback;
            if (totalFeedback > 0)
            {
                summary.HelpfulRatePercent = Math.Round(100.0 * summary.HelpfulFeedback / totalFeedback, 1);
            }

            // ---- Chat: unanswered questions (fallback answers) ----
            summary.FallbackAnswers = await _db.ChatMessages.CountAsync(m => m.IsFallback);
            List<ChatMessage> fallbackMessages = await _db.ChatMessages
                .Where(m => m.IsFallback)
                .OrderByDescending(m => m.CreatedAt)
                .Take(10)
                .ToListAsync();

            foreach (ChatMessage fallback in fallbackMessages)
            {
                // the question is the customer message right before the fallback answer
                ChatMessage question = await _db.ChatMessages
                    .Where(m => m.ConversationId == fallback.ConversationId && m.Role == "User" && m.Id < fallback.Id)
                    .OrderByDescending(m => m.Id)
                    .FirstOrDefaultAsync();

                if (question != null)
                {
                    UnansweredQuestionDto item = new UnansweredQuestionDto();
                    item.ConversationId = fallback.ConversationId;
                    item.Question = question.Content;
                    item.AskedAt = question.CreatedAt;
                    summary.UnansweredQuestions.Add(item);
                }
            }

            // ---- Knowledge base ----
            summary.ApprovedDocuments = await _db.KnowledgeDocuments.CountAsync(d => d.ApprovalStatus == ApprovalStatus.Approved && d.ProcessingStatus == ProcessingStatus.Indexed);
            summary.FailedDocuments = await _db.KnowledgeDocuments.CountAsync(d => d.ProcessingStatus == ProcessingStatus.Failed);

            return summary;
        }
    }
}
