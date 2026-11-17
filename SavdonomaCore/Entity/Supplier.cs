namespace Savdonoma.Core.Entity
{
    // Yetkazib beruvchi
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Inn { get; set; }
        public string? Note { get; set; }
        public bool IsActive { get; set; } = true;

        // Bizning unga qarzimiz (so'm): kirimda oshadi, to'lovda kamayadi
        public long Debt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
    }
}
