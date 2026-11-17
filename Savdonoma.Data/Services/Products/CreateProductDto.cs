using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Products
{
    public class CreateProductDto
    {
        public Unit Unit { get; set; }
        public long Price { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string? Barcode { get; set; }
        public string Name { get; set; } = null!;
        public string NameSearch { get; set; } = null!;

        // Boshlang'ich qoldiq (qoldiq rejimida)
        public decimal Quantity { get; set; }

        // Ixtiyoriy qo'shimcha ma'lumotlar
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
