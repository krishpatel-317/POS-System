//Individual products inside that bill
namespace POSSystem.Models
{
    public class SaleItem
    {
        public int SaleItemId { get; set; }

        // Sale Foreign Key
        public int SaleId { get; set; }

        // Sale Navigation Property
        public Sale Sale { get; set; } = null!;

        // Product Foreign Key
        public int ProductId { get; set; }

        // Product Navigation Property
        public Product Product { get; set; } = null!;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }
    }
}