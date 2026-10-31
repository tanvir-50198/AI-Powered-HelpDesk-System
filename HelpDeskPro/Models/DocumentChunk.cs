namespace HelpDeskPro.Models
{
    // A small piece of a document that can be searched
    public class DocumentChunk
    {
        public int Id { get; set; }

        public int DocumentId { get; set; }
        public KnowledgeDocument Document { get; set; }

        public int ChunkIndex { get; set; }

        // The heading the chunk belongs to
        public string SectionLabel { get; set; }

        public string Content { get; set; }

        // The embedding vector (stored as a PostgreSQL real[] column)
        public float[] Embedding { get; set; }

        // Which embedding model created the vector
        public string EmbeddingModel { get; set; }

        public string MetadataJson { get; set; }
    }
}
