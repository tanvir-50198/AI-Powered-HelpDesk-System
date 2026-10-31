using System.Text;

namespace HelpDeskPro.Services.RAG
{
    public class TextChunk
    {
        public int Index { get; set; }
        public string SectionLabel { get; set; }
        public string Content { get; set; }
    }

    public interface ITextChunker
    {
        List<TextChunk> Split(string text, int chunkSize, int overlap);
    }

    // Splits a long text into smaller pieces ("chunks").
    //   - A new Markdown heading (# Title) always starts a new chunk
    //   - A chunk is closed when it reaches the chunk size
    //   - The next chunk repeats the last few characters (overlap) so no sentence is lost
    public class TextChunker : ITextChunker
    {
        public List<TextChunk> Split(string text, int chunkSize, int overlap)
        {
            List<TextChunk> chunks = new List<TextChunk>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return chunks;
            }
            if (chunkSize < 200)
            {
                chunkSize = 200;
            }
            if (overlap < 0 || overlap >= chunkSize / 2)
            {
                overlap = 0;
            }

            string[] lines = text.Replace("\r\n", "\n").Split('\n');

            string currentSection = "";
            StringBuilder current = new StringBuilder();
            bool hasNewText = false;

            foreach (string rawLine in lines)
            {
                string line = rawLine.TrimEnd();

                // 1) Heading: close the running chunk and remember the new section name
                if (line.StartsWith("#"))
                {
                    CloseChunk(chunks, current, currentSection, 0, ref hasNewText);
                    currentSection = line.TrimStart('#').Trim();
                    continue;
                }

                // 2) Empty line: only a paragraph break
                if (line.Length == 0)
                {
                    current.Append("\n");
                    continue;
                }

                // 3) Very long line: cut it into pieces
                while (line.Length > chunkSize)
                {
                    int cut = line.LastIndexOf(' ', chunkSize - 1);
                    if (cut <= 0)
                    {
                        cut = chunkSize;
                    }
                    current.Append(line.Substring(0, cut)).Append("\n");
                    hasNewText = true;
                    CloseChunk(chunks, current, currentSection, overlap, ref hasNewText);
                    line = line.Substring(cut).TrimStart();
                }

                // 4) Normal line
                current.Append(line).Append("\n");
                hasNewText = true;

                if (current.Length >= chunkSize)
                {
                    CloseChunk(chunks, current, currentSection, overlap, ref hasNewText);
                }
            }

            CloseChunk(chunks, current, currentSection, 0, ref hasNewText);
            return chunks;
        }

        // Saves the text collected so far as a chunk and prepares the next one
        private static void CloseChunk(List<TextChunk> chunks, StringBuilder current, string section, int overlap, ref bool hasNewText)
        {
            string content = current.ToString().Trim();
            current.Clear();

            if (content.Length > 0 && hasNewText)
            {
                TextChunk chunk = new TextChunk();
                chunk.Index = chunks.Count;
                chunk.SectionLabel = section;
                chunk.Content = content;
                chunks.Add(chunk);

                // start the next chunk with the tail of this one
                if (overlap > 0 && content.Length > overlap)
                {
                    string tail = content.Substring(content.Length - overlap);
                    int firstSpace = tail.IndexOf(' ');
                    if (firstSpace >= 0 && firstSpace < tail.Length - 1)
                    {
                        tail = tail.Substring(firstSpace + 1);
                    }
                    current.Append(tail).Append("\n");
                }
            }

            hasNewText = false;
        }
    }
}
