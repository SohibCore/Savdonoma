namespace Savdonoma.Data.Services.Reports
{
    public class TopProductDto
    {
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public long Amount { get; set; }
    }
}
