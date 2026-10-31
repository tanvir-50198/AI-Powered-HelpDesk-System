using HelpDeskPro.Models;

namespace HelpDeskPro.Services.RAG
{
    // One search hit: the chunk plus the scores that explain why it was selected
    public class RetrievedChunk
    {
        public DocumentChunk Chunk { get; set; }
        public double VectorScore { get; set; }
        public double KeywordScore { get; set; }
        public int KeywordHits { get; set; }
        public double FinalScore { get; set; }

        public string DocumentTitle
        {
            get { return Chunk.Document.Title; }
        }
    }
}
