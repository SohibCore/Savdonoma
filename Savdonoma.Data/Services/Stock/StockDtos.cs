using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;

namespace Savdonoma.Data.Services.Stock
{
    public class StockLineInput
    {
        public int ProductId { get; set; }

        // Kirim / hisobdan chiqarish: miqdor (> 0). Inventarizatsiya: sanalgan miqdor (>= 0).
        public decimal Quantity { get; set; }

        // Faqat kirimda: sotib olish narxi (1 birlik)
        public long CostPrice { get; set; }

        // Faqat kirimda, ixtiyoriy: yangi sotuv narxi
        public long? NewSalePrice { get; set; }
    }

    public class StockDocumentInput
    {
        public int? SupplierId { get; set; }
        public string? Note { get; set; }
        public List<StockLineInput> Lines { get; set; } = new();
    }

    public class StockDocumentLineDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public Unit Unit { get; set; }
        public decimal Quantity { get; set; }
        public long CostPrice { get; set; }
        public long LineTotal { get; set; }
        public long? NewSalePrice { get; set; }

        // Kirimda solishtirish uchun: mahsulotning hozirgi sotuv narxi
        public long CurrentSalePrice { get; set; }

        // Yangi narxning kirim narxiga nisbatan ustamasi va tannarxdan arzonligi
        public decimal? NewMarkupPercent => NewSalePrice is null ? null : PriceCalculator.MarkupPercent(NewSalePrice.Value, CostPrice);
        public bool IsBelowCost => NewSalePrice is not null && NewSalePrice < CostPrice;

        // Inventarizatsiya: o'tkazilgan hujjatda tizimdagi qoldiq, qoralamada joriy qoldiq
        public decimal? ExpectedQuantity { get; set; }
        public decimal? Difference => ExpectedQuantity is null ? null : Quantity - ExpectedQuantity;
    }

    public class StockDocumentDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public StockDocumentType Type { get; set; }
        public StockDocumentStatus Status { get; set; }
        public int? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? Note { get; set; }
        public long TotalCost { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PostedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? CancelReason { get; set; }
        public List<StockDocumentLineDto> Lines { get; set; } = new();
    }

    public class StockDocumentListItemDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public StockDocumentType Type { get; set; }
        public StockDocumentStatus Status { get; set; }
        public string? SupplierName { get; set; }
        public long TotalCost { get; set; }
        public int LineCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PostedAt { get; set; }
    }

    public class StockDocumentFilter
    {
        public StockDocumentType? Type { get; set; }
        public StockDocumentStatus? Status { get; set; }
        public int? SupplierId { get; set; }

        // Mahalliy sanalar (kun bo'yicha, ikkalasi ham kiradi)
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class StockMovementDto
    {
        public int Id { get; set; }
        public StockMovementType Type { get; set; }
        public decimal Quantity { get; set; }
        public decimal BalanceAfter { get; set; }
        public long CostPrice { get; set; }

        // Mahalliy vaqt
        public DateTime CreatedAt { get; set; }
        public string? DocumentNumber { get; set; }
        public string? SaleNumber { get; set; }
        public string? Note { get; set; }
    }

    public class LowStockDto
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public Unit Unit { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinQuantity { get; set; }
    }
}
