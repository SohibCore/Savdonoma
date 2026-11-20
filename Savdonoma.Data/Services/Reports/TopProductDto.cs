using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Reports
{
    public class TopProductDto
    {
        public string ProductName { get; set; } = null!;
        public Unit Unit { get; set; }
        public decimal Quantity { get; set; }
        public long Amount { get; set; }
    }
}
