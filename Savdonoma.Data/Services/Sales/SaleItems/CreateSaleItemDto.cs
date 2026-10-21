namespace Savdonoma.Data.Services.Sales.SaleItems
{
    public class CreateSaleItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}