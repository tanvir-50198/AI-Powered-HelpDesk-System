namespace HelpDeskPro.Services.RAG
{
    public static class VectorMath
    {
        // Cosine similarity between two vectors (1 = same direction, 0 = unrelated)
        public static double CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length || a.Length == 0)
            {
                return 0;
            }

            double dot = 0;
            double lengthA = 0;
            double lengthB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                lengthA += a[i] * a[i];
                lengthB += b[i] * b[i];
            }

            if (lengthA == 0 || lengthB == 0)
            {
                return 0;
            }

            return dot / (Math.Sqrt(lengthA) * Math.Sqrt(lengthB));
        }
    }
}
