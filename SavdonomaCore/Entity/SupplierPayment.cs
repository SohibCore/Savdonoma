namespace Savdonoma.Core.Entity
{
    // Yetkazib beruvchiga to'langan pul
    public class SupplierPayment
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public long Amount { get; set; }
        public string? Note { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Supplier Supplier { get; set; } = null!;
    }
}
