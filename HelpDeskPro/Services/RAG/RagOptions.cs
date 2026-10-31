namespace HelpDeskPro.Services.RAG
{
    // Settings read from the "Rag" section of appsettings.json
    public class RagOptions
    {
        // Minimum score the best chunk needs, otherwise the bot says "I don't know"
        public double MinScore { get; set; } = 0.65;

        // How many chunks are given to the answer generator
        public int TopK { get; set; } = 4;

        // Chunk size in characters, and how many characters repeat in the next chunk
        public int ChunkSize { get; set; } = 800;
        public int ChunkOverlap { get; set; } = 120;

        public int MaxQuestionLength { get; set; } = 1000;

        // Leave the key empty to use the built-in "extractive" answer mode (no AI provider needed)
        public string OpenAiApiKey { get; set; }
        public string OpenAiModel { get; set; } = "gpt-4o-mini";
    }
}
