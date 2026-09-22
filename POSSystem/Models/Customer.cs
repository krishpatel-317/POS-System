using System.ComponentModel.DataAnnotations;

// Person buying
namespace POSSystem.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        // One Customer -> Many Sales
        public ICollection<Sale> Sales { get; set; }
            = new List<Sale>();
    }
}