using HelpDeskPro.Data;
using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskPro.Controllers.Api
{
    [Route("api/admin")]
    [Authorize(Roles = Roles.Admin)]
    public class AdminApiController : ApiControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IDocumentIngestionService _ingestionService;
        private readonly IReportService _reportService;
        private readonly ILogger<AdminApiController> _logger;

        public AdminApiController(
            ApplicationDbContext db,
            IDocumentIngestionService ingestionService,
            IReportService reportService,
            ILogger<AdminApiController> logger)
        {
            _db = db;
            _ingestionService = ingestionService;
            _reportService = reportService;
            _logger = logger;
        }

        // POST /api/admin/knowledge-documents   (multipart/form-data)
        [HttpPost("knowledge-documents")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<ActionResult<DocumentDto>> Upload([FromForm] UploadDocumentRequest request)
        {
            ServiceResult<KnowledgeDocument> result;
            using (Stream stream = request.File.OpenReadStream())
            {
                result = await _ingestionService.CreateFromUploadAsync(request.File.FileName, stream, request.Title, request.Category, request.Audience);
            }
            if (!result.Success)
            {
                return ErrorResult(result);
            }

            _logger.LogInformation("AUDIT: admin {AdminId} uploaded document {DocumentId}", CurrentUser.UserId, result.Data.Id);
            return StatusCode(201, DocumentDto.FromEntity(result.Data));
        }

        // GET /api/admin/knowledge-documents
        [HttpGet("knowledge-documents")]
        public async Task<ActionResult<List<DocumentDto>>> GetDocuments()
        {
            List<KnowledgeDocument> documents = await _db.KnowledgeDocuments
                .OrderBy(d => d.Category).ThenBy(d => d.Title)
                .ToListAsync();

            List<DocumentDto> list = new List<DocumentDto>();
            foreach (KnowledgeDocument document in documents)
            {
                list.Add(DocumentDto.FromEntity(document));
            }
            return Ok(list);
        }

        // POST /api/admin/knowledge-documents/3/reindex
        [HttpPost("knowledge-documents/{id}/reindex")]
        public async Task<ActionResult> Reindex(int id)
        {
            ServiceResult result = await _ingestionService.IndexDocumentAsync(id);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            _logger.LogInformation("AUDIT: admin {AdminId} re-indexed document {DocumentId}", CurrentUser.UserId, id);
            return NoContent();
        }

        // POST /api/admin/knowledge-documents/3/approve
        [HttpPost("knowledge-documents/{id}/approve")]
        public async Task<ActionResult> Approve(int id)
        {
            ServiceResult result = await _ingestionService.SetApprovalAsync(id, ApprovalStatus.Approved);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            _logger.LogInformation("AUDIT: admin {AdminId} approved document {DocumentId}", CurrentUser.UserId, id);
            return NoContent();
        }

        // POST /api/admin/knowledge-documents/3/reject
        [HttpPost("knowledge-documents/{id}/reject")]
        public async Task<ActionResult> Reject(int id)
        {
            ServiceResult result = await _ingestionService.SetApprovalAsync(id, ApprovalStatus.Rejected);
            if (!result.Success)
            {
                return ErrorResult(result);
            }
            _logger.LogInformation("AUDIT: admin {AdminId} rejected document {DocumentId}", CurrentUser.UserId, id);
            return NoContent();
        }

        // GET /api/admin/reports/summary
        [HttpGet("reports/summary")]
        public async Task<ActionResult<AdminSummaryDto>> GetSummary()
        {
            AdminSummaryDto summary = await _reportService.GetSummaryAsync();
            return Ok(summary);
        }
    }
}
