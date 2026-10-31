using Microsoft.AspNetCore.Identity;

namespace HelpDeskPro.Models
{
    // Extends the standard Identity user with a few extra fields
    public class ApplicationUser : IdentityUser
    {
        public string FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
