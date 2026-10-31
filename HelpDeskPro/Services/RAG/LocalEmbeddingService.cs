namespace HelpDeskPro.Services.RAG
{
    // A simple embedding that works OFFLINE (no API key, no cost).
    //
    // How it works ("hashing trick"):
    //   every word is converted to a number, the number picks one of 512 slots,
    //   and the slot counter goes up. Texts that share words get similar vectors.
    //
    // It does not understand synonyms like a real AI embedding model does.
    // To upgrade, create another class that implements IEmbeddingService
    // (for example one that calls an embedding API) and register it in Program.cs.
    public class LocalEmbeddingService : IEmbeddingService
    {
        private const int Dimensions = 512;

        public string ModelName
        {
            get { return "local-hash-512"; }
        }

        public Task<float[]> CreateEmbeddingAsync(string text)
        {
            float[] vector = new float[Dimensions];

            List<string> words = TextHelper.Tokenize(text);
            foreach (string word in words)
            {
                if (TextHelper.IsStopWord(word))
                {
                    continue;
                }
                int slot = (int)(StableHash(word) % Dimensions);
                vector[slot] += 1f;
            }

            // Make the vector length 1 so that similarity is easy to compare
            double sumOfSquares = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                sumOfSquares += vector[i] * vector[i];
            }
            if (sumOfSquares > 0)
            {
                float length = (float)Math.Sqrt(sumOfSquares);
                for (int i = 0; i < vector.Length; i++)
                {
                    vector[i] = vector[i] / length;
                }
            }

            return Task.FromResult(vector);
        }

        // string.GetHashCode() changes on every application start,
        // so we use our own stable hash (FNV-1a).
        private static uint StableHash(string text)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (char c in text)
                {
                    hash = hash ^ c;
                    hash = hash * 16777619;
                }
                return hash;
            }
        }
    }
}
