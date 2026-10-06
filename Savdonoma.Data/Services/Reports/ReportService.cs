using Savdonoma.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ReportService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }
        private static (DateTime fromUtc, DateTime toUtc) ToUtcRange(DateTime from, DateTime to)
        {
            return (from.Date.ToUniversalTime(), to.Date.AddDays(1).ToUniversalTime());
        }

        public async Task<SalesSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken cancellation)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            using var db = _factory.CreateDbContext();

            var rows = await db.Sales.AsNoTracking()
                .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
                .Select(s => new
                {
                    s.SaleStatus,
                    s.PaymentMethod,
                    s.TotalAmount
                }).ToListAsync(cancellation);

            var done = rows.Where(r => r.SaleStatus == SaleStatus.Completed).ToList();
            var total = done.Sum(r => r.TotalAmount);

            return new SalesSummaryDto
            {
                TotalAmount = total,
                SalesCount = done.Count,
                CashAmount = done.Where(r => r.PaymentMethod == PaymentMethod.Naqd).Sum(r => r.TotalAmount),
                CardAmount = done.Where(r => r.PaymentMethod == PaymentMethod.Karta).Sum(r => r.TotalAmount),
                AverageCheck = done.Count == 0 ? 0 : total / done.Count,
                CancelledCount = rows.Count - done.Count
            };
        }

        public async Task<List<SaleRowDto>> GetSalesAsync(DateTime from, DateTime to, CancellationToken cancellation)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            using var db = _factory.CreateDbContext();

            var rows = await db.Sales.AsNoTracking()
                .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.Id, s.Number, s.CreatedAt, s.PaymentMethod, s.TotalAmount, s.SaleStatus })
                .ToListAsync(cancellation);

            return rows.Select(r => new SaleRowDto
            {
                Id = r.Id,
                Number = r.Number,
                Time = (r.CreatedAt ?? DateTime.UtcNow).ToLocalTime(),
                PaymentMethod = r.PaymentMethod,
                TotalAmount = r.TotalAmount,
                Status = r.SaleStatus
            }).ToList();
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, CancellationToken cancellation, int take = 10)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            using var db = _factory.CreateDbContext();

            var rows = await db.SaleItems.AsNoTracking()
                .Where(i => i.Sale.SaleStatus == SaleStatus.Completed
                         && i.Sale.CreatedAt >= fromUtc && i.Sale.CreatedAt < toUtc)
                .Select(i => new { i.ProductId, i.ProductName, i.Quantity, i.LineTotal })
                .ToListAsync(cancellation);

            return rows
                .GroupBy(r => r.ProductId)
                .Select(g => new TopProductDto
                {
                    ProductName = g.First().ProductName,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Amount)
                .Take(take)
                .ToList();
        }
    }
}
