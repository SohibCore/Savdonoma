using Savdonoma.Core.Enums;
using Savdonoma.Data.Services.Sales.SaleItems;

namespace Savdonoma.Data.Services.Sales
{
    public class CreateSaleDto
    {
        public PaymentMethod PaymentMethod { get; set; }
        public List<CreateSaleItemDto> Items { get; set; } = new List<CreateSaleItemDto>();
    }
}
