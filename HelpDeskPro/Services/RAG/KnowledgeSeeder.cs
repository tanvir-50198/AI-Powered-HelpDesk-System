using HelpDeskPro.Data;
using HelpDeskPro.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Services.RAG
{
    public interface IKnowledgeSeeder
    {
        // Imports every new .md file from the KnowledgeBase folder. Returns how many were imported.
        Task<int> ImportKnowledgeBaseFolderAsync();
    }

    // Loads the starter documents (KnowledgeBase/*.md) into the database.
    // Files that are already imported are skipped, so it is safe to run again.
    public class KnowledgeSeeder : IKnowledgeSeeder
    {
        private readonly ApplicationDbContext _db;
        private readonly AppPaths _paths;
        private readonly IDocumentIngestionService _ingestionService;
        private readonly ILogger<KnowledgeSeeder> _logger;

        public KnowledgeSeeder(ApplicationDbContext db, AppPaths paths, IDocumentIngestionService ingestionService, ILogger<KnowledgeSeeder> logger)
        {
            _db = db;
            _paths = paths;
            _ingestionService = ingestionService;
            _logger = logger;
        }

        public async Task<int> ImportKnowledgeBaseFolderAsync()
        {
            string folder = _paths.KnowledgeBaseFolder;
            if (!Directory.Exists(folder))
            {
                _logger.LogWarning("KnowledgeBase folder not found: {Folder}", folder);
                return 0;
            }

            List<string> alreadyImported = await _db.KnowledgeDocuments
                .Where(d => d.IsSeedFile)
                .Select(d => d.StoragePath)
                .ToListAsync();

            string[] files = Directory.GetFiles(folder, "*.md", SearchOption.AllDirectories);
            Array.Sort(files);

            int importedCount = 0;
            foreach (string file in files)
            {
                if (Path.GetFileName(file).ToLowerInvariant() == "readme.md")
                {
                    continue;
                }

                string relativePath = Path.GetRelativePath(folder, file).Replace('\\', '/');
                if (alreadyImported.Contains(relativePath))
                {
                    continue;
                }

                string text = File.ReadAllText(file);
                FrontMatterResult parsed = FrontMatter.Parse(text);

                KnowledgeDocument document = new KnowledgeDocument();
                document.Title = GetTitle(parsed, file);
                document.SourceType = "md";
                document.StoragePath = relativePath;
                document.IsSeedFile = true;
                document.Category = GetValue(parsed, "category", GetFolderName(relativePath));
                document.Audience = ParseAudience(GetValue(parsed, "audience", "Customer"));
                document.ApprovalStatus = ParseApproval(GetValue(parsed, "status", "Approved"));
                document.ProcessingStatus = ProcessingStatus.Pending;
                document.UpdatedAt = DateTime.UtcNow;

                _db.KnowledgeDocuments.Add(document);
                await _db.SaveChangesAsync();

                await _ingestionService.IndexDocumentAsync(document.Id);
                importedCount++;
            }

            _logger.LogInformation("Knowledge base import finished. Imported {Count} documents.", importedCount);
            return importedCount;
        }

        private string GetTitle(FrontMatterResult parsed, string file)
        {
            if (parsed.Meta.ContainsKey("title") && parsed.Meta["title"].Length > 0)
            {
                return parsed.Meta["title"];
            }

            // use the first "# Heading" if there is one
            string[] lines = parsed.Body.Replace("\r\n", "\n").Split('\n');
            foreach (string line in lines)
            {
                if (line.StartsWith("# "))
                {
                    return line.Substring(2).Trim();
                }
            }
            return Path.GetFileNameWithoutExtension(file);
        }

        private string GetValue(FrontMatterResult parsed, string key, string defaultValue)
        {
            if (parsed.Meta.ContainsKey(key) && parsed.Meta[key].Length > 0)
            {
                return parsed.Meta[key];
            }
            return defaultValue;
        }

        private string GetFolderName(string relativePath)
        {
            int slash = relativePath.IndexOf('/');
            if (slash > 0)
            {
                return relativePath.Substring(0, slash);
            }
            return "General";
        }

        // "Customer" -> Customer, "Agent" -> Agent, "Agent/Internal" or "Internal" -> Internal
        private DocumentAudience ParseAudience(string value)
        {
            string text = value.ToLowerInvariant();
            if (text.Contains("internal") || text.Contains("admin"))
            {
                return DocumentAudience.Internal;
            }
            if (text.Contains("agent"))
            {
                return DocumentAudience.Agent;
            }
            return DocumentAudience.Customer;
        }

        private ApprovalStatus ParseApproval(string value)
        {
            string text = value.ToLowerInvariant();
            if (text == "approved")
            {
                return ApprovalStatus.Approved;
            }
            if (text == "rejected")
            {
                return ApprovalStatus.Rejected;
            }
            return ApprovalStatus.Draft;
        }
    }
}
