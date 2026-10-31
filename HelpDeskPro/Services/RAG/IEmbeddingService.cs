namespace HelpDeskPro.Services.RAG
{
    public interface IEmbeddingService
    {
        // Name stored in the database next to every vector
        string ModelName { get; }

        Task<float[]> CreateEmbeddingAsync(string text);
    }
}
