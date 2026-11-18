using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Products
{
    public class ProductDto
    {
        public int Id { get; set; }
        public Unit Unit { get; set; }
        public long Price { get; set; }
        public bool IsActive { get; set; }
        public int? CategoryId { get; set; }
        public string? Barcode { get; set; }
        public string Name { get; set; } = null!;
        public string NameSearch { get; set; } = null!;
        public decimal Quantity { get; set; }

        public long CostPrice { get; set; }
        public decimal? MarkupPercent { get; set; }
        public decimal? MinQuantity { get; set; }
        public string? ScaleCode { get; set; }
        public string? MxikCode { get; set; }
        public string? PackageCode { get; set; }
        public decimal? VatPercent { get; set; }
        public bool IsMarked { get; set; }

        public bool IsLowStock => MinQuantity is not null && Quantity <= MinQuantity;

        // Ro'yxatda ko'rsatish uchun: tannarx noma'lum bo'lsa "—"
        public string CostPriceText => CostPrice > 0
            ? CostPrice.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', ' ')
            : "—";

        // Haqiqiy ustama: joriy narx tannarxga nisbatan
        public string ActualMarkupText => Savdonoma.Core.Logic.PriceCalculator.MarkupPercent(Price, CostPrice) is { } m
            ? $"{m:0.#}%"
            : "—";

        public bool IsBelowCost => CostPrice > 0 && Price < CostPrice;
    }

    // Barcode bo'yicha qidiruv natijasi. Tarozi barcode'ida vazn ham keladi.
    public class BarcodeLookupResult
    {
        public ProductDto Product { get; set; } = null!;

        // Tarozi barcode'i bo'lsa: vazn (kg). Oddiy barcode'da null.
        public decimal? Quantity { get; set; }
    }
}
