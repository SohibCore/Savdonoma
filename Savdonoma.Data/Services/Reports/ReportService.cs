using Savdonoma.Services;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Reports
{
    public class ReportService : IReportService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;

        public ReportService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser)
        {
            _factory = factory;
            _currentUser = currentUser;
        }

        // Sotuvchi faqat o'z sotuvlarini ko'radi, rahbar va menejer hammasini
        private IQueryable<Sale> VisibleSales(AppDbContext db)
        {
            var sales = db.Sales.AsNoTracking();
            if (_currentUser.CanViewAllSales)
                return sales;

            var userId = _currentUser.UserId ?? -1; // tizimga kirmagan bo'lsa hech narsa ko'rinmaydi
            return sales.Where(s => s.UserId == userId);
        }
        private static (DateTime fromUtc, DateTime toUtc) ToUtcRange(DateTime from, DateTime to)
        {
            var localFrom = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
            var localTo = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Unspecified);

            return (
                TimeZoneInfo.ConvertTimeToUtc(localFrom, TimeZoneInfo.Local),
                TimeZoneInfo.ConvertTimeToUtc(localTo, TimeZoneInfo.Local));
        }

        public async Task<SalesSummaryDto> GetSummaryAsync(DateTime from, DateTime to, CancellationToken cancellation)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var rows = await VisibleSales(db)
                .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
                .Select(s => new
                {
                    s.SaleStatus,
                    s.PaymentMethod,
                    s.TotalAmount
                }).ToListAsync(cancellation);

            // Summalar faqat tugallangan sotuvlardan hisoblanadi, bekor qilinganlar alohida sanaladi
            var done = rows.Where(r => r.SaleStatus == SaleStatus.Completed).ToList();
            var total = done.Sum(r => r.TotalAmount);

            return new SalesSummaryDto
            {
                TotalAmount = total,
                SalesCount = done.Count,
                CashAmount = done.Where(r => r.PaymentMethod == PaymentMethod.Naqd).Sum(r => r.TotalAmount),
                CardAmount = done.Where(r => r.PaymentMethod == PaymentMethod.Karta).Sum(r => r.TotalAmount),
                AverageCheck = done.Count == 0 ? 0 : total / done.Count,
                CancelledCount = rows.Count(r => r.SaleStatus == SaleStatus.Cancelled)
            };
        }

        public async Task<List<SaleRowDto>> GetSalesAsync(DateTime from, DateTime to, CancellationToken cancellation)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            using var db = await _factory.CreateDbContextAsync(cancellation);

            var rows = await VisibleSales(db)
                .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.Number,
                    s.CreatedAt,
                    s.PaymentMethod,
                    s.TotalAmount,
                    s.SaleStatus
                })
                .ToListAsync(cancellation);

            return rows.Select(r => new SaleRowDto
            {
                Id = r.Id,
                Number = r.Number,
                Time = (r.CreatedAt).ToLocalTime(),
                PaymentMethod = r.PaymentMethod,
                TotalAmount = r.TotalAmount,
                Status = r.SaleStatus
            }).ToList();
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, CancellationToken cancellation, int take = 10)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var visibleSales = VisibleSales(db)
                .Where(s => s.SaleStatus == SaleStatus.Completed
                         && s.CreatedAt >= fromUtc && s.CreatedAt < toUtc);

            var rows = await visibleSales
                .SelectMany(s => s.SaleItems)
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
