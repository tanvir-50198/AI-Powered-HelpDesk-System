namespace HelpDeskPro.Services.RAG
{
    // Reads the small metadata block at the top of a Markdown file:
    //
    // ---
    // title: How do I reset my password?
    // category: FAQ
    // audience: Customer
    // status: Approved
    // ---
    // (document text...)
    public class FrontMatterResult
    {
        public Dictionary<string, string> Meta { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; }
    }

    public static class FrontMatter
    {
        public static FrontMatterResult Parse(string text)
        {
            FrontMatterResult result = new FrontMatterResult();
            result.Body = text;

            if (string.IsNullOrEmpty(text))
            {
                result.Body = "";
                return result;
            }

            string[] lines = text.Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0 || lines[0].Trim() != "---")
            {
                return result;
            }

            // find the closing ---
            int endLine = -1;
            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].Trim() == "---")
                {
                    endLine = i;
                    break;
                }
            }
            if (endLine == -1)
            {
                return result;
            }

            for (int i = 1; i < endLine; i++)
            {
                int colon = lines[i].IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }
                string key = lines[i].Substring(0, colon).Trim().ToLowerInvariant();
                string value = lines[i].Substring(colon + 1).Trim().Trim('"');
                result.Meta[key] = value;
            }

            result.Body = string.Join("\n", lines, endLine + 1, lines.Length - endLine - 1);
            return result;
        }
    }
}
