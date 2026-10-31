using HelpDeskPro.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Data
{
    // The database context: one DbSet for every table
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketComment> TicketComments { get; set; }
        public DbSet<TicketHistory> TicketHistory { get; set; }

        public DbSet<KnowledgeDocument> KnowledgeDocuments { get; set; }
        public DbSet<DocumentChunk> DocumentChunks { get; set; }

        public DbSet<ChatConversation> ChatConversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatSource> ChatSources { get; set; }
        public DbSet<ChatFeedback> ChatFeedback { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ---- Tickets ----
            builder.Entity<Ticket>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.AssignedAgent)
                .WithMany()
                .HasForeignKey(t => t.AssignedAgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ticket>().HasIndex(t => t.Status);
            builder.Entity<Ticket>().HasIndex(t => t.CreatedByUserId);
            builder.Entity<Ticket>().HasIndex(t => t.AssignedAgentId);

            builder.Entity<TicketComment>()
                .HasOne(c => c.Ticket)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TicketComment>()
                .HasOne(c => c.Author)
                .WithMany()
                .HasForeignKey(c => c.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TicketHistory>()
                .HasOne(h => h.Ticket)
                .WithMany(t => t.History)
                .HasForeignKey(h => h.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<TicketHistory>()
                .HasOne(h => h.Actor)
                .WithMany()
                .HasForeignKey(h => h.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ---- Knowledge base ----
            builder.Entity<DocumentChunk>()
                .HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DocumentChunk>()
                .HasIndex(c => new { c.DocumentId, c.ChunkIndex });

            // ---- Chat ----
            builder.Entity<ChatConversation>()
                .HasOne(c => c.CustomerUser)
                .WithMany()
                .HasForeignKey(c => c.CustomerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChatConversation>().HasIndex(c => c.CustomerUserId);

            builder.Entity<ChatMessage>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatSource>()
                .HasOne(s => s.Message)
                .WithMany(m => m.Sources)
                .HasForeignKey(s => s.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatFeedback>()
                .HasOne(f => f.Message)
                .WithMany(m => m.Feedback)
                .HasForeignKey(f => f.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChatFeedback>()
                .HasOne(f => f.CustomerUser)
                .WithMany()
                .HasForeignKey(f => f.CustomerUserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
