using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Reports
{
    public class SaleRowDto
    {
        public int Id { get; set; }
        public string Number { get; set; } = "";
        public DateTime Time { get; set; }          
        public PaymentMethod PaymentMethod { get; set; }
        public long TotalAmount { get; set; }
        public SaleStatus Status { get; set; }
    }
}
