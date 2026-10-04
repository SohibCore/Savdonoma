using System.ComponentModel.DataAnnotations.Schema;

namespace Savdonoma.Core.Entity
{
    public class SaleItem
    {
        public int Id { get; set; }
        public int SaleId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public long UnitPrice { get; set; }
        public decimal Quantity { get; set; }
        public long LineTotal { get; set; }
        public long TotalAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey(nameof(SaleId))]
        public Sale Sale { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public Product Product { get; set; } = null!;
    }
}
