namespace HelpDeskPro.Services.RAG
{
    public interface IRerankingService
    {
        List<RetrievedChunk> Rerank(string question, List<RetrievedChunk> candidates, int topK);
    }

    // Second look at the candidates.
    // A chunk whose document title / section name contains words of the question gets a small bonus.
    // This is a simple rule-based reranker; a real reranking model could replace it later.
    public class RerankingService : IRerankingService
    {
        private const double TitleBonus = 0.10;
        private const double SectionBonus = 0.05;

        public List<RetrievedChunk> Rerank(string question, List<RetrievedChunk> candidates, int topK)
        {
            List<string> queryWords = TextHelper.GetKeywords(question);

            foreach (RetrievedChunk candidate in candidates)
            {
                List<string> titleWords = TextHelper.Tokenize(candidate.Chunk.Document.Title);
                List<string> sectionWords = TextHelper.Tokenize(candidate.Chunk.SectionLabel);

                bool titleMatches = false;
                bool sectionMatches = false;

                foreach (string word in queryWords)
                {
                    if (titleWords.Contains(word))
                    {
                        titleMatches = true;
                    }
                    if (sectionWords.Contains(word))
                    {
                        sectionMatches = true;
                    }
                }

                if (titleMatches)
                {
                    candidate.FinalScore += TitleBonus;
                }
                if (sectionMatches)
                {
                    candidate.FinalScore += SectionBonus;
                }
            }

            return candidates
                .OrderByDescending(c => c.FinalScore)
                .Take(topK)
                .ToList();
        }
    }
}
