using System.Text.RegularExpressions;

namespace HelpDeskPro.Services.RAG
{
    // Shared text tools used by the embedding service and the retriever.
    // Both must "see" words in exactly the same way, so the code lives in one place.
    public static class TextHelper
    {
        // Letters, marks (needed for Bangla) and digits make a word
        private static readonly Regex WordRegex = new Regex(@"[\p{L}\p{M}\p{N}]+", RegexOptions.Compiled);

        private static readonly HashSet<string> StopWords = new HashSet<string>
        {
            "a", "an", "the", "is", "are", "was", "were", "be", "been", "am",
            "do", "does", "did", "can", "could", "should", "would", "will", "shall", "may",
            "i", "me", "my", "we", "our", "you", "your", "he", "she", "it", "its", "they", "them", "their",
            "to", "of", "in", "on", "at", "for", "from", "by", "with", "about", "into", "as", "this", "that",
            "and", "or", "but", "if", "then", "so", "not", "no", "yes",
            "what", "which", "who", "when", "where", "why", "how", "there", "here", "any", "some",
            "have", "has", "had", "get", "got", "need", "want", "please", "tell",
            "এই", "এটা", "এটি", "কি", "কী", "কেন", "কিভাবে", "কীভাবে", "আমি", "আমার", "করে", "করা",
            "হয়", "না", "এবং", "বা", "থেকে", "জন্য", "কোন", "কোনো", "আছে", "হবে", "করতে"
        };

        // Splits text into normalized lower-case words (stop words are kept)
        public static List<string> Tokenize(string text)
        {
            List<string> words = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return words;
            }

            MatchCollection matches = WordRegex.Matches(text.ToLowerInvariant());
            foreach (Match match in matches)
            {
                words.Add(NormalizeWord(match.Value));
            }
            return words;
        }

        // Same as Tokenize, but without stop words and without duplicates.
        // These are the "important" words of a question.
        public static List<string> GetKeywords(string text)
        {
            List<string> keywords = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
            {
                return keywords;
            }

            MatchCollection matches = WordRegex.Matches(text.ToLowerInvariant());
            foreach (Match match in matches)
            {
                if (StopWords.Contains(match.Value))
                {
                    continue;
                }
                string word = NormalizeWord(match.Value);
                if (word.Length < 2)
                {
                    continue;
                }
                if (!keywords.Contains(word))
                {
                    keywords.Add(word);
                }
            }
            return keywords;
        }

        public static bool IsStopWord(string word)
        {
            return StopWords.Contains(word);
        }

        // A very small "stemmer" for English words:
        // tickets -> ticket, creating -> creat, created -> creat, create -> creat
        // Bangla words and numbers are returned unchanged.
        public static string NormalizeWord(string word)
        {
            if (!IsAsciiLetters(word))
            {
                return word;
            }

            string result = word;

            if (result.Length > 4 && result.EndsWith("ies"))
            {
                result = result.Substring(0, result.Length - 3) + "y";
            }
            else if (result.Length > 5 && result.EndsWith("ing"))
            {
                result = result.Substring(0, result.Length - 3);
            }
            else if (result.Length > 4 && result.EndsWith("ed"))
            {
                result = result.Substring(0, result.Length - 2);
            }
            else if (result.Length > 3 && result.EndsWith("s") && !result.EndsWith("ss"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            // resetting -> resett -> reset
            if (result.Length > 3)
            {
                char last = result[result.Length - 1];
                char beforeLast = result[result.Length - 2];
                if (last == beforeLast && last != 's' && last != 'l')
                {
                    result = result.Substring(0, result.Length - 1);
                }
            }

            // create -> creat
            if (result.Length > 4 && result.EndsWith("e"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

        private static bool IsAsciiLetters(string word)
        {
            foreach (char c in word)
            {
                if (c < 'a' || c > 'z')
                {
                    return false;
                }
            }
            return word.Length > 0;
        }
    }
}
