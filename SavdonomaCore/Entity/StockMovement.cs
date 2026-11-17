using Savdonoma.Core.Enums;

namespace Savdonoma.Core.Entity
{
    // Ombor harakatlari jurnali: qoldiqning har bir o'zgarishi alohida yozuv.
    // Product.Quantity = shu mahsulot harakatlari yig'indisi.
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public StockMovementType Type { get; set; }

        // + kirim, - chiqim
        public decimal Quantity { get; set; }

        // Harakatdan keyingi qoldiq
        public decimal BalanceAfter { get; set; }

        // Harakat paytidagi tannarx (1 birlik)
        public long CostPrice { get; set; }

        public int? DocumentId { get; set; }
        public int? SaleId { get; set; }
        public int? UserId { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
        public StockDocument? Document { get; set; }
        public Sale? Sale { get; set; }
    }
}
