using System.ComponentModel.DataAnnotations;
using HelpDeskPro.Models;

namespace HelpDeskPro.DTOs
{
    // Used by the admin upload form and by POST /api/admin/knowledge-documents
    public class UploadDocumentRequest
    {
        [StringLength(250)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Category { get; set; }

        public DocumentAudience Audience { get; set; } = DocumentAudience.Customer;

        [Required(ErrorMessage = "Please choose a file.")]
        public IFormFile File { get; set; }
    }
}
