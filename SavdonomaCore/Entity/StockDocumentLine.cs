namespace Savdonoma.Core.Entity
{
    public class StockDocumentLine
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public int ProductId { get; set; }

        // Kirim va hisobdan chiqarishda: miqdor (> 0).
        // Inventarizatsiyada: haqiqatda sanalgan miqdor (>= 0).
        public decimal Quantity { get; set; }

        // Kirimda: sotib olish narxi (1 birlik). Boshqa hujjatlarda: o'tkazish paytidagi tannarx.
        public long CostPrice { get; set; }

        // Kirimda ixtiyoriy: yangi sotuv narxi (o'tkazilganda mahsulot narxi yangilanadi)
        public long? NewSalePrice { get; set; }

        // Inventarizatsiyada: o'tkazish paytida tizimdagi qoldiq
        public decimal? ExpectedQuantity { get; set; }

        public StockDocument Document { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }
}
