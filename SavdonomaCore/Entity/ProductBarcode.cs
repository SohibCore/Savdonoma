namespace Savdonoma.Core.Entity
{
    // Mahsulotning qo'shimcha barcode'lari (asosiysi Product.Barcode'da)
    public class ProductBarcode
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Barcode { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Product Product { get; set; } = null!;
    }
}
