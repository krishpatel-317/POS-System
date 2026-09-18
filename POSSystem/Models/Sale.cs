//One complete bill/transaction
using System.Net.ServerSentEvents;

namespace POSSystem.Models
{
    public class Sale
    {
        public int SaleId { get; set; }

        public DateTime SaleDate { get; set; }

        public decimal TotalAmount { get; set; }

        // Customer Foreign Key
        public int? CustomerId { get; set; }

        // Customer Navigation Property
        public Customer? Customer { get; set; }

        // User/Cashier Foreign Key
        public int UserId { get; set; }

        // User Navigation Property
        public User User { get; set; } = null!;

        // One Sale -> Many SaleItems
        public ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();

        // One Sale -> One Payment
        public Payment Payment { get; set; } = null!;
    }
}