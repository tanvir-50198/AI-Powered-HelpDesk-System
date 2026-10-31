using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    // Information about one source document (the text itself lives in DocumentChunks)
    public class KnowledgeDocument
    {
        public int Id { get; set; }

        [Required]
        [StringLength(250)]
        public string Title { get; set; }

        // md, txt, pdf or docx
        [StringLength(10)]
        public string SourceType { get; set; }

        // Seed documents: path inside the KnowledgeBase folder.
        // Uploaded documents: file name inside the upload folder.
        [StringLength(500)]
        public string StoragePath { get; set; }

        // true = file comes from the KnowledgeBase folder, false = uploaded by an admin
        public bool IsSeedFile { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        public DocumentAudience Audience { get; set; } = DocumentAudience.Customer;
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Draft;
        public ProcessingStatus ProcessingStatus { get; set; } = ProcessingStatus.Pending;

        [StringLength(500)]
        public string ProcessingError { get; set; }

        public int Version { get; set; } = 1;

        // SHA-256 of the file content, used to detect changes
        [StringLength(100)]
        public string Checksum { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    }
}
