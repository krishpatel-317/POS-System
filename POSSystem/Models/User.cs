using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Person using the POS
namespace POSSystem.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        // Foreign Key
        [ForeignKey("Role")]
        public int RoleId { get; set; }

        public Role? Role { get; set; }

        // One User -> Many Sales
        public ICollection<Sale> Sales { get; set; }
            = new List<Sale>();
    }
}