//Type of employee
namespace POSSystem.Models
{
    public class Role
    {
        public int RoleId { get; set; }

        public string Name { get; set; } = string.Empty;

        // One Role -> Many Users
        public ICollection<User> Users { get; set; }
            = new List<User>();
    }
}