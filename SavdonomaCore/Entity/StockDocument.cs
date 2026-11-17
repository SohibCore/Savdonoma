using Savdonoma.Core.Enums;

namespace Savdonoma.Core.Entity
{
    // Ombor hujjati: kirim, hisobdan chiqarish yoki inventarizatsiya
    public class StockDocument
    {
        public int Id { get; set; }
        public string Number { get; set; } = null!;
        public StockDocumentType Type { get; set; }
        public StockDocumentStatus Status { get; set; }
        public int? SupplierId { get; set; }
        public string? Note { get; set; }

        // Kirim uchun: tannarx bo'yicha jami summa (so'm)
        public long TotalCost { get; set; }

        public DateTime CreatedAt { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime? PostedAt { get; set; }
        public int? PostedByUserId { get; set; }
        public DateTime? CancelledAt { get; set; }
        public int? CancelledByUserId { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Supplier? Supplier { get; set; }
        public ICollection<StockDocumentLine> Lines { get; set; } = new List<StockDocumentLine>();
    }
}
