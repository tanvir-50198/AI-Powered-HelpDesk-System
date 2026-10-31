using HelpDeskPro.Data;
using HelpDeskPro.Models;
using HelpDeskPro.Services.RAG;
using HelpDeskPro.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Tests
{
    // Shared code for all tests: a fresh in-memory database per test
    public static class TestHelper
    {
        public static ApplicationDbContext CreateDatabase()
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            ApplicationDbContext db = new ApplicationDbContext(options);

            db.Users.Add(new ApplicationUser { Id = "customer1", UserName = "c1@test.com", Email = "c1@test.com" });
            db.Users.Add(new ApplicationUser { Id = "customer2", UserName = "c2@test.com", Email = "c2@test.com" });
            db.Users.Add(new ApplicationUser { Id = "agent1", UserName = "a1@test.com", Email = "a1@test.com" });
            db.Users.Add(new ApplicationUser { Id = "agent2", UserName = "a2@test.com", Email = "a2@test.com" });
            db.Users.Add(new ApplicationUser { Id = "admin1", UserName = "admin@test.com", Email = "admin@test.com" });
            db.Categories.Add(new Category { Id = 1, Name = "General" });
            db.SaveChanges();
            return db;
        }

        public static UserContext Customer(string id)
        {
            return new UserContext { UserId = id, Role = Roles.Customer };
        }

        public static UserContext Agent(string id)
        {
            return new UserContext { UserId = id, Role = Roles.Agent };
        }

        public static UserContext Admin()
        {
            return new UserContext { UserId = "admin1", Role = Roles.Admin };
        }

        // Adds an indexed document with chunks (same code path as the real ingestion)
        public static async Task AddDocumentAsync(ApplicationDbContext db, string title, string text, DocumentAudience audience, ApprovalStatus approval)
        {
            KnowledgeDocument document = new KnowledgeDocument();
            document.Title = title;
            document.SourceType = "md";
            document.StoragePath = title + ".md";
            document.Category = "Test";
            document.Audience = audience;
            document.ApprovalStatus = approval;
            document.ProcessingStatus = ProcessingStatus.Indexed;
            db.KnowledgeDocuments.Add(document);
            await db.SaveChangesAsync();

            TextChunker chunker = new TextChunker();
            LocalEmbeddingService embedding = new LocalEmbeddingService();
            foreach (TextChunk piece in chunker.Split(text, 800, 120))
            {
                DocumentChunk chunk = new DocumentChunk();
                chunk.DocumentId = document.Id;
                chunk.ChunkIndex = piece.Index;
                chunk.SectionLabel = piece.SectionLabel;
                chunk.Content = piece.Content;
                chunk.Embedding = await embedding.CreateEmbeddingAsync(title + ". " + piece.SectionLabel + ". " + piece.Content);
                chunk.EmbeddingModel = embedding.ModelName;
                db.DocumentChunks.Add(chunk);
            }
            await db.SaveChangesAsync();
        }
    }
}
