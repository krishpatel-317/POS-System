using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace POSSystem.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        // Standard GST tax rate for this category (e.g. 0%, 5%, 12%, 18%, 28%)
        [Required]
        [Range(0, 100, ErrorMessage = "GST rate must be between 0% and 100%")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal GSTRate { get; set; } = 18m;

        // One Category -> Many Products
        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}