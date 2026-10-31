using System.Security.Cryptography;
using HelpDeskPro.Data;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HelpDeskPro.Services.RAG
{
    public interface IDocumentIngestionService
    {
        Task<ServiceResult<KnowledgeDocument>> CreateFromUploadAsync(string originalFileName, Stream content, string title, string category, DocumentAudience audience);

        Task<ServiceResult> IndexDocumentAsync(int documentId);

        Task<ServiceResult> SetApprovalAsync(int documentId, ApprovalStatus status);

        Task<ServiceResult> DeleteDocumentAsync(int documentId);
    }

    // Ingestion pipeline:
    //   file -> extract text -> chunk -> embed -> save chunks -> mark "Indexed"
    // If any step fails the document is marked "Failed" and the old chunks stay untouched.
    public class DocumentIngestionService : IDocumentIngestionService
    {
        private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".txt", ".md" };

        private readonly ApplicationDbContext _db;
        private readonly AppPaths _paths;
        private readonly IDocumentTextExtractor _extractor;
        private readonly ITextChunker _chunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly RagOptions _options;
        private readonly ILogger<DocumentIngestionService> _logger;

        public DocumentIngestionService(
            ApplicationDbContext db,
            AppPaths paths,
            IDocumentTextExtractor extractor,
            ITextChunker chunker,
            IEmbeddingService embeddingService,
            IOptions<RagOptions> options,
            ILogger<DocumentIngestionService> logger)
        {
            _db = db;
            _paths = paths;
            _extractor = extractor;
            _chunker = chunker;
            _embeddingService = embeddingService;
            _options = options.Value;
            _logger = logger;
        }

        // ------------------------------------------------------------
        // Upload
        // ------------------------------------------------------------
        public async Task<ServiceResult<KnowledgeDocument>> CreateFromUploadAsync(string originalFileName, Stream content, string title, string category, DocumentAudience audience)
        {
            if (string.IsNullOrWhiteSpace(originalFileName))
            {
                return ServiceResult<KnowledgeDocument>.Fail(ErrorType.Validation, "Please choose a file.");
            }

            // 1) Validate the file type
            string extension = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (Array.IndexOf(AllowedExtensions, extension) < 0)
            {
                return ServiceResult<KnowledgeDocument>.Fail(ErrorType.Validation, "Only PDF, DOCX, TXT and MD files are allowed.");
            }

            // 2) Save the file (stop if it is too big)
            Directory.CreateDirectory(_paths.UploadFolder);
            string storedName = Guid.NewGuid().ToString("N") + extension;
            string fullPath = Path.Combine(_paths.UploadFolder, storedName);

            long totalBytes = 0;
            byte[] buffer = new byte[81920];
            bool tooBig = false;
            using (FileStream output = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                int read = await content.ReadAsync(buffer, 0, buffer.Length);
                while (read > 0)
                {
                    totalBytes += read;
                    if (totalBytes > _paths.MaxUploadBytes)
                    {
                        tooBig = true;
                        break;
                    }
                    await output.WriteAsync(buffer, 0, read);
                    read = await content.ReadAsync(buffer, 0, buffer.Length);
                }
            }
            if (tooBig)
            {
                File.Delete(fullPath);
                return ServiceResult<KnowledgeDocument>.Fail(ErrorType.Validation, "The file is too large.");
            }
            if (totalBytes == 0)
            {
                File.Delete(fullPath);
                return ServiceResult<KnowledgeDocument>.Fail(ErrorType.Validation, "The file is empty.");
            }

            // 3) Create the database record (Draft: not visible to the chatbot until approved)
            KnowledgeDocument document = new KnowledgeDocument();
            document.Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(originalFileName) : title.Trim();
            document.SourceType = extension.TrimStart('.');
            document.StoragePath = storedName;
            document.IsSeedFile = false;
            document.Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            document.Audience = audience;
            document.ApprovalStatus = ApprovalStatus.Draft;
            document.ProcessingStatus = ProcessingStatus.Pending;
            document.UpdatedAt = DateTime.UtcNow;

            _db.KnowledgeDocuments.Add(document);
            await _db.SaveChangesAsync();

            // 4) Extract, chunk, embed
            await IndexDocumentAsync(document.Id);

            // Reload so the caller sees the final status
            KnowledgeDocument saved = await _db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == document.Id);
            return ServiceResult<KnowledgeDocument>.Ok(saved);
        }

        // ------------------------------------------------------------
        // Index / Re-index
        // ------------------------------------------------------------
        public async Task<ServiceResult> IndexDocumentAsync(int documentId)
        {
            KnowledgeDocument document = await _db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null)
            {
                return ServiceResult.Fail(ErrorType.NotFound, "Document not found.");
            }

            try
            {
                string path = _paths.GetDocumentPath(document);
                if (!File.Exists(path))
                {
                    throw new FileNotFoundException("The source file was not found on the server.");
                }

                // Checksum tells us whether the file changed since the last indexing
                byte[] fileBytes = File.ReadAllBytes(path);
                string checksum = Convert.ToHexString(SHA256.HashData(fileBytes));

                string text = _extractor.ExtractText(path, document.SourceType);
                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new InvalidOperationException("No readable text was found. (Scanned PDFs need OCR, which is not part of the MVP.)");
                }

                List<TextChunk> pieces = _chunker.Split(text, _options.ChunkSize, _options.ChunkOverlap);
                if (pieces.Count == 0)
                {
                    throw new InvalidOperationException("The document did not produce any chunks.");
                }

                // Create all new chunks first, so a failure cannot destroy the old index
                List<DocumentChunk> newChunks = new List<DocumentChunk>();
                foreach (TextChunk piece in pieces)
                {
                    string textForEmbedding = document.Title + ". " + piece.SectionLabel + ". " + piece.Content;

                    DocumentChunk chunk = new DocumentChunk();
                    chunk.DocumentId = document.Id;
                    chunk.ChunkIndex = piece.Index;
                    chunk.SectionLabel = piece.SectionLabel;
                    chunk.Content = piece.Content;
                    chunk.Embedding = await _embeddingService.CreateEmbeddingAsync(textForEmbedding);
                    chunk.EmbeddingModel = _embeddingService.ModelName;
                    chunk.MetadataJson = "{\"category\":\"" + document.Category + "\",\"version\":" + document.Version + "}";
                    newChunks.Add(chunk);
                }

                // Replace the old chunks. SaveChanges is one database transaction,
                // so readers see either the old chunks or the new chunks, never a mix.
                List<DocumentChunk> oldChunks = await _db.DocumentChunks.Where(c => c.DocumentId == document.Id).ToListAsync();
                _db.DocumentChunks.RemoveRange(oldChunks);
                _db.DocumentChunks.AddRange(newChunks);

                if (document.Checksum != null && document.Checksum != checksum)
                {
                    document.Version = document.Version + 1;
                }
                document.Checksum = checksum;
                document.ProcessingStatus = ProcessingStatus.Indexed;
                document.ProcessingError = null;
                document.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();
                return ServiceResult.Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Indexing failed for document {DocumentId}", documentId);

                // Forget half-finished changes, then store the failure
                _db.ChangeTracker.Clear();
                KnowledgeDocument failedDocument = await _db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
                if (failedDocument != null)
                {
                    failedDocument.ProcessingStatus = ProcessingStatus.Failed;
                    failedDocument.ProcessingError = Shorten(ex.Message, 400);
                    failedDocument.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                return ServiceResult.Fail(ErrorType.Validation, "Indexing failed: " + Shorten(ex.Message, 200));
            }
        }

        // ------------------------------------------------------------
        // Approve / Reject / Delete
        // ------------------------------------------------------------
        public async Task<ServiceResult> SetApprovalAsync(int documentId, ApprovalStatus status)
        {
            KnowledgeDocument document = await _db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null)
            {
                return ServiceResult.Fail(ErrorType.NotFound, "Document not found.");
            }
            if (status == ApprovalStatus.Approved && document.ProcessingStatus != ProcessingStatus.Indexed)
            {
                return ServiceResult.Fail(ErrorType.Conflict, "Only successfully indexed documents can be approved.");
            }

            document.ApprovalStatus = status;
            document.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        public async Task<ServiceResult> DeleteDocumentAsync(int documentId)
        {
            KnowledgeDocument document = await _db.KnowledgeDocuments.FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null)
            {
                return ServiceResult.Fail(ErrorType.NotFound, "Document not found.");
            }

            // Delete the uploaded file (seed files in the KnowledgeBase folder are never deleted)
            if (!document.IsSeedFile)
            {
                string path = _paths.GetDocumentPath(document);
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }

            _db.KnowledgeDocuments.Remove(document);   // chunks are removed by the cascade rule
            await _db.SaveChangesAsync();
            return ServiceResult.Ok();
        }

        private static string Shorten(string text, int maxLength)
        {
            if (text == null)
            {
                return "";
            }
            if (text.Length <= maxLength)
            {
                return text;
            }
            return text.Substring(0, maxLength);
        }
    }
}
