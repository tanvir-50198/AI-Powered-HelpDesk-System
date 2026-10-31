using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Controllers
{
    // Admin pages to manage the chatbot knowledge base
    [Authorize(Roles = Roles.Admin)]
    public class KnowledgeDocumentsController : AppControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IDocumentIngestionService _ingestionService;
        private readonly IKnowledgeSeeder _knowledgeSeeder;
        private readonly ILogger<KnowledgeDocumentsController> _logger;

        public KnowledgeDocumentsController(
            ApplicationDbContext db,
            IDocumentIngestionService ingestionService,
            IKnowledgeSeeder knowledgeSeeder,
            ILogger<KnowledgeDocumentsController> logger)
        {
            _db = db;
            _ingestionService = ingestionService;
            _knowledgeSeeder = knowledgeSeeder;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            List<KnowledgeDocument> documents = await _db.KnowledgeDocuments
                .OrderBy(d => d.Category).ThenBy(d => d.Title)
                .ToListAsync();
            return View(documents);
        }

        public async Task<IActionResult> Details(int id)
        {
            KnowledgeDocument document = await _db.KnowledgeDocuments
                .Include(d => d.Chunks)
                .FirstOrDefaultAsync(d => d.Id == id);
            if (document == null)
            {
                return NotFound();
            }
            document.Chunks = document.Chunks.OrderBy(c => c.ChunkIndex).ToList();
            return View(document);
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View(new UploadDocumentRequest());
        }

        [HttpPost]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> Upload(UploadDocumentRequest model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ServiceResult<KnowledgeDocument> result;
            using (Stream stream = model.File.OpenReadStream())
            {
                result = await _ingestionService.CreateFromUploadAsync(model.File.FileName, stream, model.Title, model.Category, model.Audience);
            }

            if (!result.Success)
            {
                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            _logger.LogInformation("AUDIT: admin {AdminId} uploaded document {DocumentId}", CurrentUser.UserId, result.Data.Id);

            if (result.Data.ProcessingStatus == ProcessingStatus.Indexed)
            {
                TempData["Success"] = "Document uploaded and indexed. Approve it so that the chatbot can use it.";
            }
            else
            {
                TempData["Error"] = "Document uploaded, but indexing failed: " + result.Data.ProcessingError;
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Approve(int id)
        {
            ServiceResult result = await _ingestionService.SetApprovalAsync(id, ApprovalStatus.Approved);
            _logger.LogInformation("AUDIT: admin {AdminId} approved document {DocumentId}", CurrentUser.UserId, id);
            ShowResult(result, "Document approved. The chatbot can use it now.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reject(int id)
        {
            ServiceResult result = await _ingestionService.SetApprovalAsync(id, ApprovalStatus.Rejected);
            _logger.LogInformation("AUDIT: admin {AdminId} rejected document {DocumentId}", CurrentUser.UserId, id);
            ShowResult(result, "Document rejected. The chatbot will not use it.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reindex(int id)
        {
            ServiceResult result = await _ingestionService.IndexDocumentAsync(id);
            _logger.LogInformation("AUDIT: admin {AdminId} re-indexed document {DocumentId}", CurrentUser.UserId, id);
            ShowResult(result, "Document re-indexed.");
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            ServiceResult result = await _ingestionService.DeleteDocumentAsync(id);
            _logger.LogInformation("AUDIT: admin {AdminId} deleted document {DocumentId}", CurrentUser.UserId, id);
            ShowResult(result, "Document deleted.");
            return RedirectToAction("Index");
        }

        // Imports new .md files from the KnowledgeBase folder
        [HttpPost]
        public async Task<IActionResult> ImportFolder()
        {
            int count = await _knowledgeSeeder.ImportKnowledgeBaseFolderAsync();
            TempData["Success"] = count + " new document(s) imported from the KnowledgeBase folder.";
            return RedirectToAction("Index");
        }
    }
}
