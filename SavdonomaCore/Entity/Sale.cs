using Savdonoma.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Savdonoma.Core.Entity
{
    public class Sale
    {
        public int Id { get; set; }
        public string Number { get; set; } = null!;
        public long TotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public Status Status { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        //Navigation property
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}
