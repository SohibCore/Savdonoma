using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Products
{
    public class UpdateProductDto
    {
        public int Id { get; set; }
        public Unit? Unit { get; set; }
        public long? Price { get; set; }
        public bool? IsActive { get; set; }
        public int? CategoryId { get; set; }
        public string? Barcode { get; set; }
        public string? Name { get; set; }
        public string? NameSearch { get; set; }

        // Qoldiq rejimida: yangi qoldiq. Farq "Qo'lda tuzatish" harakati sifatida yoziladi.
        public decimal? Quantity { get; set; }

        // true bo'lsa, quyidagi maydonlar berilgan qiymatga o'rnatiladi (null = tozalash).
        // false bo'lsa, ular o'zgarmaydi (eski chaqiruvlar uchun).
        public bool UpdateDetails { get; set; }
        public long? CostPrice { get; set; }
        public decimal? MarkupPercent { get; set; }
        public decimal? MinQuantity { get; set; }
        public string? ScaleCode { get; set; }
        public string? MxikCode { get; set; }
        public string? PackageCode { get; set; }
        public decimal? VatPercent { get; set; }
        public bool IsMarked { get; set; }
    }
}
