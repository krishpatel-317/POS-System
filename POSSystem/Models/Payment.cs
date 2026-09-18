//Money / payment information for the bill
namespace POSSystem.Models
{
    public class Payment
    {
        public int PaymentId { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = string.Empty;

        public DateTime PaymentDate { get; set; }

        // Sale Foreign Key
        public int SaleId { get; set; }

        // Sale Navigation Property
        public Sale Sale { get; set; } = null!;
    }
}