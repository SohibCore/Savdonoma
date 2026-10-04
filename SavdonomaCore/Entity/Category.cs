namespace Savdonoma.Core.Entity
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
