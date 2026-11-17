namespace Savdonoma.Core.Entity
{
    public class SystemSettings
    {
        public int Id { get; set; }
        public bool InventoryEnabled { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ShopName { get; set; }
        public string? Address { get; set; }
        public string? PrinterName { get; set; }
        public int? CheckLength { get; set; }

        // Umumiy ustama (%): mahsulot va kategoriyada ustama bo'lmasa ishlatiladi.
        // null bo'lsa, kirimda narx avtomatik o'zgarmaydi.
        public decimal? DefaultMarkupPercent { get; set; }

        // Sotuv narxi shu qadamgacha yuqoriga yaxlitlanadi (so'm). null = 100.
        public int? PriceRoundingStep { get; set; }
    }
}
