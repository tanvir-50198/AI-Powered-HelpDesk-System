namespace HelpDeskPro.Models
{
    // Shows which document/section an answer was based on.
    // ChunkId and DocumentId are plain numbers (no foreign key) on purpose:
    // when a document is re-indexed or deleted, old chat history must stay valid.
    public class ChatSource
    {
        public int Id { get; set; }

        public int MessageId { get; set; }
        public ChatMessage Message { get; set; }

        public int ChunkId { get; set; }
        public int DocumentId { get; set; }

        public string DisplayTitle { get; set; }
        public string SectionLabel { get; set; }
    }
}
