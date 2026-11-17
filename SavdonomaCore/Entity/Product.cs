using Savdonoma.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Savdonoma.Core.Entity
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string NameSearch { get; set; } = null!;
        public Unit Unit { get; set; }

        // Sotuv narxi (so'm)
        public long Price { get; set; }
        public bool IsActive { get; set; }
        public int? CategoryId { get; set; }

        // Asosiy barcode. Qo'shimchalari Barcodes'da.
        public string? Barcode { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Joriy qoldiq = StockMovement'lar yig'indisi. Faqat StockLedger orqali o'zgartiriladi.
        public decimal Quantity { get; set; }

        // O'rtacha tannarx (1 birlik, so'm). Har bir kirimdan keyin qayta hisoblanadi.
        public long CostPrice { get; set; }

        // Mahsulotning o'z ustamasi (%). Bo'lmasa kategoriya, keyin umumiy ustama ishlatiladi.
        public decimal? MarkupPercent { get; set; }

        // Shu miqdordan kam qolsa ogohlantiriladi
        public decimal? MinQuantity { get; set; }

        // Tarozi PLU kodi (tarozi barcode'idagi mahsulot kodi)
        public string? ScaleCode { get; set; }

        // Fiskal chek uchun (4-bosqich)
        public string? MxikCode { get; set; }
        public string? PackageCode { get; set; }
        public decimal? VatPercent { get; set; }
        public bool IsMarked { get; set; }

        //Navigation property
        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
        public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
    }
}
