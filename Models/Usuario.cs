using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace mvc.Models
{
    public class Usuario : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(50)]
        public string? Position { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = false;
    }
}