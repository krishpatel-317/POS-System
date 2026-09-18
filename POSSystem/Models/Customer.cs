//Person buying
namespace POSSystem.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // One Customer -> Many Sales
        public ICollection<Sale> Sales { get; set; }
            = new List<Sale>();
    }
}