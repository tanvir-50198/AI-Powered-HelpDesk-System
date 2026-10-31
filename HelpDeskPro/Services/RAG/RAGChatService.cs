using System.Text.RegularExpressions;
using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskPro.Services.RAG
{
    public interface IRAGChatService
    {
        Task<ChatConversation> CreateConversationAsync(string customerUserId);

        Task<List<ChatConversation>> GetConversationsAsync(string customerUserId);

        Task<ServiceResult<ChatConversation>> GetConversationAsync(int conversationId, string customerUserId);

        Task<ServiceResult<ChatAnswerDto>> AskAsync(int conversationId, string customerUserId, string question);

        Task<ServiceResult> SaveFeedbackAsync(int messageId, string customerUserId, bool isHelpful, string comment);

        Task<ServiceResult<Ticket>> CreateTicketFromConversationAsync(int conversationId, string customerUserId, CreateTicketFromChatRequest request);
    }

    // The chatbot brain. Steps for every question:
    //   1. validate question and conversation owner
    //   2. hybrid retrieval (approved + customer documents only)
    //   3. rerank, then check the evidence threshold
    //   4. no evidence  -> safe fallback + "create a ticket" option
    //   5. evidence     -> generate answer, check the citations, save sources
    public class RAGChatService : IRAGChatService
    {
        private const string FallbackMessage =
            "I could not find a reliable answer to this in our knowledge base. " +
            "You can create a support ticket and a human agent will help you.";

        private readonly ApplicationDbContext _db;
        private readonly IHybridRetriever _retriever;
        private readonly IRerankingService _reranker;
        private readonly ILlmService _llmService;
        private readonly IEmbeddingService _embeddingService;
        private readonly ITicketService _ticketService;
        private readonly RagOptions _options;
        private readonly ILogger<RAGChatService> _logger;

        public RAGChatService(
            ApplicationDbContext db,
            IHybridRetriever retriever,
            IRerankingService reranker,
            ILlmService llmService,
            IEmbeddingService embeddingService,
            ITicketService ticketService,
            IOptions<RagOptions> options,
            ILogger<RAGChatService> logger)
        {
            _db = db;
            _retriever = retriever;
            _reranker = reranker;
            _llmService = llmService;
            _embeddingService = embeddingService;
            _ticketService = ticketService;
            _options = options.Value;
            _logger = logger;
        }

        // ------------------------------------------------------------
        // Conversations
        // ------------------------------------------------------------
        public async Task<ChatConversation> CreateConversationAsync(string customerUserId)
        {
            ChatConversation conversation = new ChatConversation();
            conversation.CustomerUserId = customerUserId;
            conversation.CreatedAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;

            _db.ChatConversations.Add(conversation);
            await _db.SaveChangesAsync();
            return conversation;
        }

        public async Task<List<ChatConversation>> GetConversationsAsync(string customerUserId)
        {
            return await _db.ChatConversations
                .Where(c => c.CustomerUserId == customerUserId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<ServiceResult<ChatConversation>> GetConversationAsync(int conversationId, string customerUserId)
        {
            ChatConversation conversation = await _db.ChatConversations
                .Include(c => c.Messages).ThenInclude(m => m.Sources)
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                return ServiceResult<ChatConversation>.Fail(ErrorType.NotFound, "Conversation not found.");
            }
            if (conversation.CustomerUserId != customerUserId)
            {
                return ServiceResult<ChatConversation>.Fail(ErrorType.Forbidden, "This conversation belongs to another user.");
            }

            conversation.Messages = conversation.Messages.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).ToList();
            return ServiceResult<ChatConversation>.Ok(conversation);
        }

        // ------------------------------------------------------------
        // Ask a question
        // ------------------------------------------------------------
        public async Task<ServiceResult<ChatAnswerDto>> AskAsync(int conversationId, string customerUserId, string question)
        {
            // 1) Validate
            if (string.IsNullOrWhiteSpace(question))
            {
                return ServiceResult<ChatAnswerDto>.Fail(ErrorType.Validation, "Please type a question.");
            }
            question = question.Trim();
            if (question.Length > _options.MaxQuestionLength)
            {
                return ServiceResult<ChatAnswerDto>.Fail(ErrorType.Validation, "The question is too long (maximum " + _options.MaxQuestionLength + " characters).");
            }

            ChatConversation conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation == null)
            {
                return ServiceResult<ChatAnswerDto>.Fail(ErrorType.NotFound, "Conversation not found.");
            }
            if (conversation.CustomerUserId != customerUserId)
            {
                return ServiceResult<ChatAnswerDto>.Fail(ErrorType.Forbidden, "This conversation belongs to another user.");
            }

            // 2) Save the customer's message
            ChatMessage userMessage = new ChatMessage();
            userMessage.ConversationId = conversation.Id;
            userMessage.Role = "User";
            userMessage.Content = question;
            userMessage.CreatedAt = DateTime.UtcNow;
            _db.ChatMessages.Add(userMessage);

            if (conversation.Title == "New conversation")
            {
                conversation.Title = question.Length > 50 ? question.Substring(0, 50) + "..." : question;
            }
            conversation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // 3) Retrieve + rerank. Customers can only read approved "Customer" documents.
            List<RetrievedChunk> candidates = await _retriever.SearchAsync(question, Roles.Customer, _options.TopK * 2);
            List<RetrievedChunk> bestChunks = _reranker.Rerank(question, candidates, _options.TopK);

            // 4) Is there enough evidence?
            bool hasEvidence = bestChunks.Count > 0
                && bestChunks[0].KeywordHits >= 1
                && bestChunks[0].FinalScore >= _options.MinScore;

            string answerText = FallbackMessage;
            bool isFallback = true;
            List<RetrievedChunk> usedChunks = new List<RetrievedChunk>();

            if (hasEvidence)
            {
                string generated;
                try
                {
                    generated = await _llmService.GenerateAnswerAsync(question, bestChunks);
                }
                catch (Exception ex)
                {
                    // Do not store anything and do not show technical details to the customer
                    _logger.LogError(ex, "Answer generation failed for conversation {ConversationId}", conversationId);
                    return ServiceResult<ChatAnswerDto>.Fail(ErrorType.Unavailable, "The assistant is temporarily unavailable. Please try again in a moment.");
                }

                bool notFound = string.IsNullOrWhiteSpace(generated) || generated.Trim() == "NOT_FOUND";
                if (!notFound)
                {
                    List<int> citedNumbers = new List<int>();
                    answerText = CleanCitations(generated, bestChunks.Count, citedNumbers);
                    isFallback = false;

                    // sources = chunks that the answer really cites (or the best chunk if none is cited)
                    foreach (int number in citedNumbers)
                    {
                        usedChunks.Add(bestChunks[number - 1]);
                    }
                    if (usedChunks.Count == 0)
                    {
                        usedChunks.Add(bestChunks[0]);
                    }
                }
            }

            // 5) Save the assistant message and its sources
            ChatMessage assistantMessage = new ChatMessage();
            assistantMessage.ConversationId = conversation.Id;
            assistantMessage.Role = "Assistant";
            assistantMessage.Content = answerText;
            assistantMessage.IsFallback = isFallback;
            assistantMessage.ModelInfo = _llmService.ModelName + " / " + _embeddingService.ModelName;
            assistantMessage.CreatedAt = DateTime.UtcNow;

            List<SourceDto> sourceDtos = new List<SourceDto>();
            foreach (RetrievedChunk used in usedChunks)
            {
                bool alreadyAdded = sourceDtos.Any(s => s.DocumentId == used.Chunk.DocumentId && s.Section == used.Chunk.SectionLabel);
                if (alreadyAdded)
                {
                    continue;
                }

                ChatSource source = new ChatSource();
                source.ChunkId = used.Chunk.Id;
                source.DocumentId = used.Chunk.DocumentId;
                source.DisplayTitle = used.DocumentTitle;
                source.SectionLabel = used.Chunk.SectionLabel;
                assistantMessage.Sources.Add(source);

                SourceDto sourceDto = new SourceDto();
                sourceDto.DocumentId = used.Chunk.DocumentId;
                sourceDto.Title = used.DocumentTitle;
                sourceDto.Section = used.Chunk.SectionLabel;
                sourceDtos.Add(sourceDto);
            }

            _db.ChatMessages.Add(assistantMessage);
            conversation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            ChatAnswerDto dto = new ChatAnswerDto();
            dto.ConversationId = conversation.Id;
            dto.MessageId = assistantMessage.Id;
            dto.Answer = answerText;
            dto.IsFallback = isFallback;
            dto.CanCreateTicket = isFallback;
            dto.Sources = sourceDtos;
            return ServiceResult<ChatAnswerDto>.Ok(dto);
        }

        // Keeps only citation markers that point to a real chunk, e.g. [1]..[4].
        // A marker such as [9] that does not exist is removed (it would be a fake citation).
        private string CleanCitations(string answer, int chunkCount, List<int> citedNumbers)
        {
            Regex citationRegex = new Regex(@"\[(\d+)\]");

            string cleaned = citationRegex.Replace(answer, delegate (Match match)
            {
                int number = int.Parse(match.Groups[1].Value);
                if (number >= 1 && number <= chunkCount)
                {
                    if (!citedNumbers.Contains(number))
                    {
                        citedNumbers.Add(number);
                    }
                    return match.Value;
                }
                return "";
            });

            return cleaned.Trim();
        }

        // ------------------------------------------------------------
        // Feedback
        // ------------------------------------------------------------
        public async Task<ServiceResult> SaveFeedbackAsync(int messageId, string customerUserId, bool isHelpful, string comment)
        {
            ChatMessage message = await _db.ChatMessages
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.Role != "Assistant")
            {
                return ServiceResult.Fail(ErrorType.NotFound, "Message not found.");
            }
            if (message.Conversation.CustomerUserId != customerUserId)
            {
                return ServiceResult.Fail(ErrorType.Forbidden, "This message belongs to another user.");
            }
            if (comment != null && comment.Length > 500)
            {
                return ServiceResult.Fail(ErrorType.Validation, "The comment is too long (maximum 500 characters).");
            }

            // One feedback per customer per message: update it if it already exists
            ChatFeedback feedback = await _db.ChatFeedback
                .FirstOrDefaultAsync(f => f.MessageId == messageId && f.CustomerUserId == customerUserId);

            if (feedback == null)
            {
                feedback = new ChatFeedback();
                feedback.MessageId = messageId;
                feedback.CustomerUserId = customerUserId;
                _db.ChatFeedback.Add(feedback);
            }

            feedback.IsHelpful = isHelpful;
            feedback.Comment = comment;
            feedback.CreatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        // ------------------------------------------------------------
        // Escalation: chat -> ticket
        // ------------------------------------------------------------
        public async Task<ServiceResult<Ticket>> CreateTicketFromConversationAsync(int conversationId, string customerUserId, CreateTicketFromChatRequest request)
        {
            if (!request.Confirmed)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Validation, "Please confirm the summary before the ticket is created.");
            }

            ChatConversation conversation = await _db.ChatConversations.FirstOrDefaultAsync(c => c.Id == conversationId);
            if (conversation == null)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.NotFound, "Conversation not found.");
            }
            if (conversation.CustomerUserId != customerUserId)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Forbidden, "This conversation belongs to another user.");
            }
            if (conversation.LinkedTicketId.HasValue)
            {
                return ServiceResult<Ticket>.Fail(ErrorType.Conflict, "A ticket was already created from this conversation (ticket #" + conversation.LinkedTicketId.Value + ").");
            }

            UserContext customer = new UserContext();
            customer.UserId = customerUserId;
            customer.Role = Roles.Customer;

            ServiceResult<Ticket> created = await _ticketService.CreateTicketAsync(customer, request.Title, request.Description, request.CategoryId, request.Priority);
            if (!created.Success)
            {
                return created;
            }

            conversation.LinkedTicketId = created.Data.Id;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return created;
        }
    }
}
