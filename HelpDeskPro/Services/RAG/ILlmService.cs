namespace HelpDeskPro.Services.RAG
{
    // Creates the final answer text from the retrieved chunks.
    // The answer must use citation markers like [1] [2]
    // (the number = position of the chunk in the list).
    // Returning "NOT_FOUND" means: the context does not contain the answer.
    public interface ILlmService
    {
        string ModelName { get; }

        Task<string> GenerateAnswerAsync(string question, List<RetrievedChunk> chunks);
    }
}
