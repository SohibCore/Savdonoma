using Savdonoma.Services;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Data.Services.Reports;

namespace Savdonoma.Data.Services.DashboardService
{
    public class DashboardService : IDashboardService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;

        public DashboardService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser)
        {
            _factory = factory;
            _currentUser = currentUser;
        }

        // Sotuvchi faqat o'z sotuvlarini ko'radi, rahbar va menejer hammasini (ReportService bilan bir xil)
        private IQueryable<Sale> VisibleSales(AppDbContext db)
        {
            var sales = db.Sales.AsNoTracking();
            if (_currentUser.CanViewAllSales)
                return sales;

            var userId = _currentUser.UserId ?? -1;
            return sales.Where(s => s.UserId == userId);
        }

        // Bazada CreatedAt UTC'da saqlanadi: mahalliy kunlar oralig'i [from; to] UTC'ga o'tkaziladi
        private static (DateTime fromUtc, DateTime toUtc) ToUtcRange(DateTime from, DateTime to)
        {
            var localFrom = DateTime.SpecifyKind(from.Date, DateTimeKind.Unspecified);
            var localTo = DateTime.SpecifyKind(to.Date.AddDays(1), DateTimeKind.Unspecified);

            return (
                TimeZoneInfo.ConvertTimeToUtc(localFrom, TimeZoneInfo.Local),
                TimeZoneInfo.ConvertTimeToUtc(localTo, TimeZoneInfo.Local));
        }

        public async Task<TodayDetailDto> GetDashboardDateAsync(DateTime from, DateTime to, CancellationToken cancellation)
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

            // Summalar faqat tugallangan sotuvlardan, bekor qilinganlar alohida sanaladi
            var done = rows.Where(r => r.SaleStatus == SaleStatus.Completed).ToList();
            var cash = done.Where(r => r.PaymentMethod == PaymentMethod.Naqd).ToList();
            var card = done.Where(r => r.PaymentMethod == PaymentMethod.Karta).ToList();
            var total = done.Sum(r => r.TotalAmount);

            return new TodayDetailDto
            {
                TotalAmount = total,
                SalesCount = done.Count,
                AverageCheck = done.Count == 0 ? 0 : total / done.Count,
                CancelledCount = rows.Count(r => r.SaleStatus == SaleStatus.Cancelled),

                CashCount = cash.Count,
                CashTotal = cash.Sum(r => r.TotalAmount),
                CardCount = card.Count,
                CardTotal = card.Sum(r => r.TotalAmount),
            };
        }

        public async Task<WarningDto> GetTodayWarningAsync(CancellationToken cancellation, int take = 8)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            // SQLite decimal'ni to'g'ri solishtira olmaydi, shuning uchun solishtirish xotirada
            var candidates = await db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && p.MinQuantity != null)
                .Select(p => new LowStockDto
                {
                    ProductId = p.Id,
                    Name = p.Name,
                    Unit = p.Unit,
                    Quantity = p.Quantity,
                    MinQuantity = p.MinQuantity!.Value,
                })
                .ToListAsync(cancellation);

            var low = candidates
                .Where(p => p.Quantity <= p.MinQuantity)
                .OrderBy(p => p.Quantity - p.MinQuantity)   // eng ko'p yetishmayotgani birinchi
                .ThenBy(p => p.Name)
                .ToList();

            return new WarningDto
            {
                LowStockCount = low.Count,
                OutOfStockCount = low.Count(p => p.Quantity <= 0),
                LowStockItems = low.Take(take).ToList()
            };
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(DateTime from, DateTime to, CancellationToken cancellation, int take = 5)
        {
            var (fromUtc, toUtc) = ToUtcRange(from, to);
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var sales = VisibleSales(db)
                .Where(s => s.SaleStatus == SaleStatus.Completed
                         && s.CreatedAt >= fromUtc && s.CreatedAt < toUtc);

            // decimal Sum SQLite'da ishlamaydi: qatorlar olinib, guruhlash xotirada
            var items = await sales
                .SelectMany(s => s.SaleItems)
                .Select(i => new
                {
                    i.ProductId,
                    i.ProductName,
                    i.Unit,
                    i.Quantity,
                    i.LineTotal
                }).ToListAsync(cancellation);

            return items
                .GroupBy(i => i.ProductId)
                .Select(g => new TopProductDto
                {
                    ProductName = g.First().ProductName,
                    Unit = g.First().Unit,
                    Quantity = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Amount)
                .ThenByDescending(x => x.Quantity)
                .Take(take)
                .ToList();
        }

        public async Task<List<EmployeeSalesDto>> GetEmployeeSalesAsync(DateTime from, DateTime to, CancellationToken cancellation)
        {
            // Boshqa xodimlarning savdosi faqat rahbar va menejerga ko'rinadi
            if (!_currentUser.CanViewAllSales)
                return new List<EmployeeSalesDto>();

            var (fromUtc, toUtc) = ToUtcRange(from, to);
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var sales = await db.Sales
                .AsNoTracking()
                .Where(s => s.CreatedAt >= fromUtc && s.CreatedAt < toUtc)
                .Select(s => new SaleRow(s.UserId, s.SaleStatus, s.PaymentMethod, s.TotalAmount))
                .ToListAsync(cancellation);

            var users = await db.Users
                .AsNoTracking()
                .Select(u => new { u.Id, u.FullName, u.UserRole, u.IsActive })
                .ToListAsync(cancellation);

            var byUser = sales
                .GroupBy(s => s.UserId)
                .ToDictionary(g => g.Key ?? -1, g => g.ToList());

            var result = new List<EmployeeSalesDto>();

            // Faol xodimlar savdosi bo'lmasa ham ko'rinadi (0 bilan); nofaollar faqat savdosi bo'lsa
            foreach (var user in users)
            {
                byUser.TryGetValue(user.Id, out var rows);
                if (rows is null && !user.IsActive)
                    continue;

                result.Add(Build(user.Id, user.FullName, user.UserRole, user.IsActive, rows));
            }

            // Xodimga bog'lanmagan (UserId = null) eski sotuvlar
            if (byUser.TryGetValue(-1, out var orphan))
                result.Add(Build(null, "Noma'lum xodim", null, false, orphan));

            return result
                .OrderByDescending(x => x.TotalAmount)
                .ThenByDescending(x => x.SalesCount)
                .ThenBy(x => x.FullName)
                .ToList();

            static EmployeeSalesDto Build(int? id, string name, UserRole? role, bool isActive, List<SaleRow>? rows)
            {
                var list = rows ?? new List<SaleRow>();
                var done = list.Where(r => r.SaleStatus == SaleStatus.Completed).ToList();
                var total = done.Sum(r => r.TotalAmount);

                return new EmployeeSalesDto
                {
                    UserId = id,
                    FullName = name,
                    Role = role,
                    IsActive = isActive,
                    SalesCount = done.Count,
                    TotalAmount = total,
                    AverageCheck = done.Count == 0 ? 0 : total / done.Count,
                    CashTotal = done.Where(r => r.PaymentMethod == PaymentMethod.Naqd).Sum(r => r.TotalAmount),
                    CardTotal = done.Where(r => r.PaymentMethod == PaymentMethod.Karta).Sum(r => r.TotalAmount),
                    CancelledCount = list.Count(r => r.SaleStatus == SaleStatus.Cancelled),
                };
            }
        }

        private sealed record SaleRow(int? UserId, SaleStatus SaleStatus, PaymentMethod PaymentMethod, long TotalAmount);
    }
}
