using HelpDeskPro.DTOs;
using HelpDeskPro.Models;
using HelpDeskPro.Services.Common;
using HelpDeskPro.Services.RAG;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Controllers.Api
{
    // Delete + "import starter documents" for the React knowledge-base screen
    // (the MVC KnowledgeDocumentsController already had both).
    [Route("api/admin")]
    [Authorize(Roles = Roles.Admin)]
    public class KnowledgeAdminExtrasApiController : ApiControllerBase
    {
        private readonly IDocumentIngestionService _ingestionService;
        private readonly IKnowledgeSeeder _knowledgeSeeder;
        private readonly ILogger<KnowledgeAdminExtrasApiController> _logger;

        public KnowledgeAdminExtrasApiController(
            IDocumentIngestionService ingestionService,
            IKnowledgeSeeder knowledgeSeeder,
            ILogger<KnowledgeAdminExtrasApiController> logger)
        {
            _ingestionService = ingestionService;
            _knowledgeSeeder = knowledgeSeeder;
            _logger = logger;
        }

        // DELETE /api/admin/knowledge-documents/3
        [HttpDelete("knowledge-documents/{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            ServiceResult result = await _ingestionService.DeleteDocumentAsync(id);
            if (!result.Success)
            {
                return ErrorResult(result);
            }

            _logger.LogInformation("AUDIT: admin {AdminId} deleted document {DocumentId}", CurrentUser.UserId, id);
            return NoContent();
        }

        // POST /api/admin/knowledge-documents/import-folder
        [HttpPost("knowledge-documents/import-folder")]
        public async Task<ActionResult<ImportFolderResultDto>> ImportFolder()
        {
            int count = await _knowledgeSeeder.ImportKnowledgeBaseFolderAsync();

            _logger.LogInformation("AUDIT: admin {AdminId} imported {Count} documents from the KnowledgeBase folder", CurrentUser.UserId, count);

            ImportFolderResultDto dto = new ImportFolderResultDto();
            dto.Imported = count;
            return Ok(dto);
        }
    }
}
