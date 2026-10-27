using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;

namespace Savdonoma.Data.Services.Sales
{
    public class SaleDetailDto
    {
        public string Number { get; set; } = "";
        public PaymentMethod PaymentMethod { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal? ReceivedAmount { get; set; }
        public decimal? ChangeAmount { get; set; }
        public List<SaleItem> Items { get; set; } = new();
        public int? UserId { get; set; }
        public string SellerName { get; set; } = "";
    }
}
