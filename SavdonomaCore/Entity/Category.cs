namespace Savdonoma.Core.Entity
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }

        // Kategoriya ustamasi (%). Mahsulotda o'z ustamasi bo'lmasa shu ishlatiladi.
        public decimal? MarkupPercent { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
