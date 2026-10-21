namespace Savdonoma.Core.Entity
{
    public class SystemSettings
    {
        public int Id { get; set; }
        public bool InventoryEnabled { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? ShopName { get; set; } = null!;
        public string? Address { get; set; } = null!;
        public string? PrinterName { get; set; } = null!;
        public int? CheckLength { get; set; }
    }
}
