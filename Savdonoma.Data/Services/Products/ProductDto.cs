using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Products
{
    public class ProductDto
    {
        public int Id { get; set; }
        public Unit Unit { get; set; }
        public long Price { get; set; }
        public bool IsActive { get; set; }
        public int CategoryId { get; set; }
        public string? Barcode { get; set; }
        public string Name { get; set; } = null!;
        public string NameSearch { get; set; } = null!;
    }
}
