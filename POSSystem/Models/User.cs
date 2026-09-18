//Person using the POS
namespace POSSystem.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        // Foreign Key
        public int RoleId { get; set; }

        // Navigation Property
        public Role Role { get; set; } = null!;

        // One User -> Many Sales
        public ICollection<Sale> Sales { get; set; }
            = new List<Sale>();
    }
}