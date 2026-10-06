namespace Savdonoma.Data.Services.Reports
{
    public interface IReportService
    {
        Task<SalesSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken cancellation);
        Task<List<SaleRowDto>> GetSalesAsync(DateTime from, DateTime to, CancellationToken cancellation);
        Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, CancellationToken cancellation, int take = 10);
    }
}
