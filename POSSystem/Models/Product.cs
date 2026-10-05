using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Item being sold
namespace POSSystem.Models
{
    public class Product
    {
        [Key]
        public int ProductId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        public int StockQuantity { get; set; }

        // Foreign Key
        [ForeignKey("Category")]
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        // One Product -> Many SaleItems
        public ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();
    }
}