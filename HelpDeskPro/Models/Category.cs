using System.ComponentModel.DataAnnotations;

namespace HelpDeskPro.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Description { get; set; }
    }
}
