using System.Text;

namespace HelpDeskPro.Services.RAG
{
    // Default answer generator. It does NOT call any AI provider (free, offline).
    // It quotes the best matching knowledge-base text and adds the citation numbers.
    public class ExtractiveLlmService : ILlmService
    {
        public string ModelName
        {
            get { return "extractive-no-llm"; }
        }

        public Task<string> GenerateAnswerAsync(string question, List<RetrievedChunk> chunks)
        {
            StringBuilder answer = new StringBuilder();
            answer.AppendLine("Here is what our knowledge base says:");
            answer.AppendLine();

            int numberOfPartsToShow = chunks.Count;
            if (numberOfPartsToShow > 2)
            {
                numberOfPartsToShow = 2;
            }

            for (int i = 0; i < numberOfPartsToShow; i++)
            {
                string text = chunks[i].Chunk.Content.Trim();
                if (text.Length > 600)
                {
                    text = text.Substring(0, 600) + "...";
                }
                answer.AppendLine(text + " [" + (i + 1) + "]");
                answer.AppendLine();
            }

            return Task.FromResult(answer.ToString().Trim());
        }
    }
}
