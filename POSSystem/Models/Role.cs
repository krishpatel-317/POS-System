using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // One Role -> Many Users
        public ICollection<User> Users { get; set; }
            = new List<User>();
    }
}