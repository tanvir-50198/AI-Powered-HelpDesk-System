using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using HelpDeskPro.Services.Tickets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace HelpDeskPro.Tests
{
    public class RagTests
    {
        private const string PasswordText = "# Reset password\nOpen the login page and use the password reset option. You will receive an email with a link to choose a new password.";
        private const string InternalText = "# Admin secret\nThe internal escalation password vault is used only by administrators for emergency access.";

        private async Task<ApplicationDbContext> CreateKnowledgeDatabaseAsync()
        {
            ApplicationDbContext db = TestHelper.CreateDatabase();
            await TestHelper.AddDocumentAsync(db, "How do I reset my password", PasswordText, DocumentAudience.Customer, ApprovalStatus.Approved);
            await TestHelper.AddDocumentAsync(db, "Internal vault guide", InternalText, DocumentAudience.Internal, ApprovalStatus.Approved);
            await TestHelper.AddDocumentAsync(db, "Draft billing article", "# Billing\nInvoices are sent on the first day of each month by email.", DocumentAudience.Customer, ApprovalStatus.Draft);
            return db;
        }

        private RAGChatService CreateChatService(ApplicationDbContext db)
        {
            RagOptions options = new RagOptions();
            LocalEmbeddingService embedding = new LocalEmbeddingService();
            return new RAGChatService(
                db,
                new HybridRetriever(db, embedding),
                new RerankingService(),
                new ExtractiveLlmService(),
                embedding,
                new TicketService(db, null),
                Options.Create(options),
                NullLogger<RAGChatService>.Instance);
        }

        // ---------- chunker ----------
        [Fact]
        public void Chunker_StartsNewChunkAtEveryHeading()
        {
            TextChunker chunker = new TextChunker();
            string text = "# First\nAlpha text.\n\n# Second\nBeta text.";

            List<TextChunk> chunks = chunker.Split(text, 800, 100);

            Assert.Equal(2, chunks.Count);
            Assert.Equal("First", chunks[0].SectionLabel);
            Assert.Equal("Second", chunks[1].SectionLabel);
        }

        [Fact]
        public void Chunker_SplitsLongTextAndKeepsOverlap()
        {
            TextChunker chunker = new TextChunker();
            string longText = string.Join(" ", Enumerable.Repeat("sentence about tickets and support", 80));

            List<TextChunk> chunks = chunker.Split(longText, 400, 80);

            Assert.True(chunks.Count > 1);
            Assert.All(chunks, c => Assert.True(c.Content.Length <= 600));
        }

        [Fact]
        public void FrontMatter_IsParsed()
        {
            string text = "---\ntitle: My title\naudience: Customer\n---\nBody text";

            FrontMatterResult result = FrontMatter.Parse(text);

            Assert.Equal("My title", result.Meta["title"]);
            Assert.Equal("Body text", result.Body.Trim());
        }

        // ---------- retrieval ----------
        [Fact]
        public async Task Retrieval_FindsExpectedSourceInTopResults()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            HybridRetriever retriever = new HybridRetriever(db, new LocalEmbeddingService());

            List<RetrievedChunk> hits = await retriever.SearchAsync("How can I reset my password?", Roles.Customer, 4);

            Assert.True(hits.Count > 0);
            Assert.Equal("How do I reset my password", hits[0].DocumentTitle);
        }

        [Fact]
        public async Task Retrieval_CustomerNeverGetsInternalOrDraftDocuments()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            HybridRetriever retriever = new HybridRetriever(db, new LocalEmbeddingService());

            List<RetrievedChunk> internalHits = await retriever.SearchAsync("internal escalation password vault administrators", Roles.Customer, 10);
            List<RetrievedChunk> draftHits = await retriever.SearchAsync("when are invoices sent", Roles.Customer, 10);

            Assert.DoesNotContain(internalHits, h => h.DocumentTitle == "Internal vault guide");
            Assert.DoesNotContain(draftHits, h => h.DocumentTitle == "Draft billing article");
        }

        [Fact]
        public async Task Retrieval_AdminCanReadInternalDocuments()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            HybridRetriever retriever = new HybridRetriever(db, new LocalEmbeddingService());

            List<RetrievedChunk> hits = await retriever.SearchAsync("internal escalation password vault administrators", Roles.Admin, 10);

            Assert.Contains(hits, h => h.DocumentTitle == "Internal vault guide");
        }

        // ---------- chat service ----------
        [Fact]
        public async Task Chat_AnswersWithCitation()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            RAGChatService chat = CreateChatService(db);
            ChatConversation conversation = await chat.CreateConversationAsync("customer1");

            ServiceResult<ChatAnswerDto> result = await chat.AskAsync(conversation.Id, "customer1", "How do I reset my password?");

            Assert.True(result.Success);
            Assert.False(result.Data.IsFallback);
            Assert.Contains("[1]", result.Data.Answer);
            Assert.Equal("How do I reset my password", result.Data.Sources[0].Title);
        }

        [Fact]
        public async Task Chat_AbstainsWhenKnowledgeBaseHasNoAnswer()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            RAGChatService chat = CreateChatService(db);
            ChatConversation conversation = await chat.CreateConversationAsync("customer1");

            ServiceResult<ChatAnswerDto> result = await chat.AskAsync(conversation.Id, "customer1", "What is the weather in Paris tomorrow?");

            Assert.True(result.Success);
            Assert.True(result.Data.IsFallback);
            Assert.True(result.Data.CanCreateTicket);
            Assert.Empty(result.Data.Sources);
        }

        [Fact]
        public async Task Chat_CustomerCannotUseConversationOfAnotherCustomer()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            RAGChatService chat = CreateChatService(db);
            ChatConversation conversation = await chat.CreateConversationAsync("customer1");

            ServiceResult<ChatAnswerDto> result = await chat.AskAsync(conversation.Id, "customer2", "How do I reset my password?");

            Assert.Equal(ErrorType.Forbidden, result.ErrorType);
        }

        [Fact]
        public async Task Chat_CreateTicket_NeedsConfirmation_AndWorksOnlyOnce()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            RAGChatService chat = CreateChatService(db);
            ChatConversation conversation = await chat.CreateConversationAsync("customer1");

            CreateTicketFromChatRequest request = new CreateTicketFromChatRequest();
            request.Title = "Need help";
            request.Description = "Summary of my problem";
            request.CategoryId = 1;
            request.Confirmed = false;

            ServiceResult<Ticket> notConfirmed = await chat.CreateTicketFromConversationAsync(conversation.Id, "customer1", request);
            request.Confirmed = true;
            ServiceResult<Ticket> created = await chat.CreateTicketFromConversationAsync(conversation.Id, "customer1", request);
            ServiceResult<Ticket> again = await chat.CreateTicketFromConversationAsync(conversation.Id, "customer1", request);

            Assert.Equal(ErrorType.Validation, notConfirmed.ErrorType);
            Assert.True(created.Success);
            Assert.Equal(ErrorType.Conflict, again.ErrorType);
        }

        [Fact]
        public async Task Chat_FeedbackOfAnotherCustomerIsRejected()
        {
            ApplicationDbContext db = await CreateKnowledgeDatabaseAsync();
            RAGChatService chat = CreateChatService(db);
            ChatConversation conversation = await chat.CreateConversationAsync("customer1");
            ServiceResult<ChatAnswerDto> answer = await chat.AskAsync(conversation.Id, "customer1", "How do I reset my password?");

            ServiceResult other = await chat.SaveFeedbackAsync(answer.Data.MessageId, "customer2", true, null);
            ServiceResult owner = await chat.SaveFeedbackAsync(answer.Data.MessageId, "customer1", true, "thanks");

            Assert.Equal(ErrorType.Forbidden, other.ErrorType);
            Assert.True(owner.Success);
        }
    }
}
