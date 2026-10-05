namespace Savdonoma.Data.Services.Sales
{
    public interface ISaleService
    {
        Task<SaleDto> CreateAsync(CreateSaleDto dto, CancellationToken cancellation);
        Task<List<SaleDto>> GetTodayAsync(CancellationToken cancellation);
        Task CancelAsync(int saleId, string? reason, CancellationToken cancellation);
    }
}
