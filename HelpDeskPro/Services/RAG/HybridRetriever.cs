using HelpDeskPro.Data;
using HelpDeskPro.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Services.RAG
{
    public interface IHybridRetriever
    {
        Task<List<RetrievedChunk>> SearchAsync(string question, string userRole, int maxResults);
    }

    // Hybrid search = vector similarity (meaning) + keyword match (exact words).
    //
    //   FinalScore = 0.6 * VectorScore + 0.4 * KeywordScore
    //
    // SECURITY: the approval / audience filter is part of the database query,
    // so a chunk the user is not allowed to read is never loaded and never reaches the AI.
    public class HybridRetriever : IHybridRetriever
    {
        private const double VectorWeight = 0.6;
        private const double KeywordWeight = 0.4;

        private readonly ApplicationDbContext _db;
        private readonly IEmbeddingService _embeddingService;

        public HybridRetriever(ApplicationDbContext db, IEmbeddingService embeddingService)
        {
            _db = db;
            _embeddingService = embeddingService;
        }

        public async Task<List<RetrievedChunk>> SearchAsync(string question, string userRole, int maxResults)
        {
            List<RetrievedChunk> results = new List<RetrievedChunk>();

            List<string> queryWords = TextHelper.GetKeywords(question);
            if (queryWords.Count == 0)
            {
                return results;
            }

            float[] queryVector = await _embeddingService.CreateEmbeddingAsync(question);
            List<DocumentAudience> allowedAudiences = GetAllowedAudiences(userRole);

            // Only approved + indexed documents for the allowed audiences
            List<DocumentChunk> chunks = await _db.DocumentChunks
                .Include(c => c.Document)
                .Where(c => c.Document.ApprovalStatus == ApprovalStatus.Approved
                         && c.Document.ProcessingStatus == ProcessingStatus.Indexed
                         && allowedAudiences.Contains(c.Document.Audience))
                .ToListAsync();

            foreach (DocumentChunk chunk in chunks)
            {
                RetrievedChunk hit = new RetrievedChunk();
                hit.Chunk = chunk;

                // Vector part
                double vectorScore = VectorMath.CosineSimilarity(queryVector, chunk.Embedding);
                if (vectorScore < 0)
                {
                    vectorScore = 0;
                }
                hit.VectorScore = vectorScore;

                // Keyword part: how many of the question words appear in title + section + text?
                string searchableText = chunk.Document.Title + " " + chunk.SectionLabel + " " + chunk.Content;
                List<string> chunkWords = TextHelper.Tokenize(searchableText);
                int hits = 0;
                foreach (string queryWord in queryWords)
                {
                    if (chunkWords.Contains(queryWord))
                    {
                        hits++;
                    }
                }
                hit.KeywordHits = hits;
                hit.KeywordScore = (double)hits / queryWords.Count;

                hit.FinalScore = VectorWeight * hit.VectorScore + KeywordWeight * hit.KeywordScore;
                results.Add(hit);
            }

            return results
                .Where(r => r.KeywordHits > 0 || r.VectorScore > 0.2)
                .OrderByDescending(r => r.FinalScore)
                .Take(maxResults)
                .ToList();
        }

        // Which document audiences may this role read?
        private List<DocumentAudience> GetAllowedAudiences(string userRole)
        {
            List<DocumentAudience> allowed = new List<DocumentAudience>();
            allowed.Add(DocumentAudience.Customer);

            if (userRole == Roles.Agent || userRole == Roles.Admin)
            {
                allowed.Add(DocumentAudience.Agent);
            }
            if (userRole == Roles.Admin)
            {
                allowed.Add(DocumentAudience.Internal);
            }
            return allowed;
        }
    }
}
