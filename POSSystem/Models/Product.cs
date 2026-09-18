using System.Net.ServerSentEvents;
//Item being sold
namespace POSSystem.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int StockQuantity { get; set; }

        // Foreign Key
        public int CategoryId { get; set; }

        // Navigation Property
        public Category Category { get; set; } = null!;

        // One Product -> Many SaleItems
        public ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();
    }
}