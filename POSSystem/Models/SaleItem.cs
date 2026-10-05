using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// Individual products inside that bill
namespace POSSystem.Models
{
    public class SaleItem
    {
        [Key]
        public int SaleItemId { get; set; }

        [ForeignKey("Sale")]
        public int SaleId { get; set; }

        public Sale? Sale { get; set; }

        [ForeignKey("Product")]
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
    }
}