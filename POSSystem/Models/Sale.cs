using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// One complete bill/transaction
namespace POSSystem.Models
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }

        [Required]
        public DateTime SaleDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxPercentage { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TaxAmount { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        // Sale Status: "Completed" or "Voided"
        [Required]
        public string Status { get; set; } = "Completed";

        [ForeignKey("Customer")]
        public int? CustomerId { get; set; }

        public Customer? Customer { get; set; }

        // User/Cashier Foreign Key (IdentityUser) - nullable so past sales are retained if staff user is deleted
        [ForeignKey("User")]
        public string? UserId { get; set; }

        public Microsoft.AspNetCore.Identity.IdentityUser? User { get; set; }

        // One Sale -> Many SaleItems
        public ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();

        // One Sale -> One Payment
        public Payment Payment { get; set; } = null!;
    }
}