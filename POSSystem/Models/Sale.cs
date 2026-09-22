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
        public decimal TotalAmount { get; set; }

        [ForeignKey("Customer")]
        public int? CustomerId { get; set; }

        public Customer? Customer { get; set; }

        // User/Cashier Foreign Key
        [ForeignKey("User")]
        public int UserId { get; set; }

        public User User { get; set; }

        // One Sale -> Many SaleItems
        public ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();

        // One Sale -> One Payment
        public Payment Payment { get; set; } = null!;
    }
}