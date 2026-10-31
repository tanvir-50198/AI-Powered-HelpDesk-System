using HelpDeskPro.Models;

namespace HelpDeskPro.Services.RAG
{
    // Knows where the knowledge files are stored on disk
    public class AppPaths
    {
        public string KnowledgeBaseFolder { get; private set; }
        public string UploadFolder { get; private set; }
        public long MaxUploadBytes { get; private set; }

        public AppPaths(IWebHostEnvironment environment, IConfiguration configuration)
        {
            // Seed documents: <project>/KnowledgeBase (or next to the published files)
            string knowledgeFolder = Path.Combine(environment.ContentRootPath, "KnowledgeBase");
            if (!Directory.Exists(knowledgeFolder))
            {
                knowledgeFolder = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase");
            }
            KnowledgeBaseFolder = knowledgeFolder;

            // Uploaded documents are kept OUTSIDE wwwroot so nobody can download them by URL
            string uploadSetting = configuration["Uploads:Folder"];
            if (string.IsNullOrWhiteSpace(uploadSetting))
            {
                uploadSetting = "App_Data/uploads";
            }
            UploadFolder = Path.Combine(environment.ContentRootPath, uploadSetting);

            int maxMb = 5;
            string maxMbSetting = configuration["Uploads:MaxFileSizeMb"];
            if (!string.IsNullOrWhiteSpace(maxMbSetting))
            {
                int.TryParse(maxMbSetting, out maxMb);
            }
            if (maxMb < 1)
            {
                maxMb = 5;
            }
            MaxUploadBytes = (long)maxMb * 1024 * 1024;
        }

        // Full path of the original file of a knowledge document
        public string GetDocumentPath(KnowledgeDocument document)
        {
            if (document.IsSeedFile)
            {
                return Path.Combine(KnowledgeBaseFolder, document.StoragePath);
            }
            return Path.Combine(UploadFolder, document.StoragePath);
        }
    }
}
