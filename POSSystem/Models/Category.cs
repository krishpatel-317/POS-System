//Group of products
using System.ComponentModel.DataAnnotations;

namespace POSSystem.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // One Category -> Many Products
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}