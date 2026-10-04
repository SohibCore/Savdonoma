using Savdonoma.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Savdonoma.Core.Entity
{
    public class Product
    {
        public int Id { get; set; }
        public Unit Unit { get; set; }
        public long Price { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string? Barcode { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Name { get; set; } = null!;
        public string NameSearch { get; set; } = null!;

        //Navigation property
        [ForeignKey(nameof(CategoryId))]
        public Category Category { get; set; } = null!;
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}