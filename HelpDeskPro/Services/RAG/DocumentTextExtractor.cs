using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;

namespace HelpDeskPro.Services.RAG
{
    public interface IDocumentTextExtractor
    {
        string ExtractText(string filePath, string sourceType);
    }

    // Reads plain text out of md / txt / pdf / docx files
    public class DocumentTextExtractor : IDocumentTextExtractor
    {
        public string ExtractText(string filePath, string sourceType)
        {
            string type = (sourceType ?? "").ToLowerInvariant();

            if (type == "md")
            {
                string markdown = File.ReadAllText(filePath, Encoding.UTF8);
                return FrontMatter.Parse(markdown).Body;
            }
            if (type == "txt")
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }
            if (type == "pdf")
            {
                return ExtractPdf(filePath);
            }
            if (type == "docx")
            {
                return ExtractDocx(filePath);
            }

            throw new InvalidOperationException("Unsupported file type: " + sourceType);
        }

        private string ExtractPdf(string filePath)
        {
            StringBuilder text = new StringBuilder();
            using (PdfDocument pdf = PdfDocument.Open(filePath))
            {
                foreach (UglyToad.PdfPig.Content.Page page in pdf.GetPages())
                {
                    text.AppendLine(page.Text);
                    text.AppendLine();
                }
            }
            return text.ToString();
        }

        private string ExtractDocx(string filePath)
        {
            StringBuilder text = new StringBuilder();
            using (WordprocessingDocument document = WordprocessingDocument.Open(filePath, false))
            {
                Body body = document.MainDocumentPart.Document.Body;
                foreach (Paragraph paragraph in body.Descendants<Paragraph>())
                {
                    string styleName = "";
                    if (paragraph.ParagraphProperties != null
                        && paragraph.ParagraphProperties.ParagraphStyleId != null
                        && paragraph.ParagraphProperties.ParagraphStyleId.Val != null)
                    {
                        styleName = paragraph.ParagraphProperties.ParagraphStyleId.Val.Value;
                    }

                    string line = paragraph.InnerText;
                    if (styleName != null && styleName.StartsWith("Heading") && line.Trim().Length > 0)
                    {
                        text.AppendLine("# " + line);
                    }
                    else
                    {
                        text.AppendLine(line);
                    }
                    text.AppendLine();
                }
            }
            return text.ToString();
        }
    }
}
