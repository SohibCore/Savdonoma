using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic; // Ensure the correct namespace for SaleItem is included

namespace Savdonoma.Data.Services.Sales
{
    public class SaleService : ISaleService
    {
        private static readonly SemaphoreSlim SaleCreationLock = new(1, 1);
        private readonly IDbContextFactory<AppDbContext> _factory;
        public SaleService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<SaleDto> CreateAsync(CreateSaleDto dto, CancellationToken cancellation)
        {
            if (dto.Items.Count == 0)
                throw new Exception("Savat bo'sh.");

            if (dto.Items.Any(q => q.Quantity <= 0))
                throw new Exception("Miqdor 0 dan katta bo'lilshi kerak.");

            await SaleCreationLock.WaitAsync(cancellation);
            try
            {
                for (var attempt = 0; ; attempt++)
                {
                    try
                    {
                        return await CreateOnceAsync(dto, cancellation);
                    }
                    catch (Exception ex) when (IsDatabaseLocked(ex) && attempt < 2)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(150 * (attempt + 1)), cancellation);
                    }
                }
            }
            finally
            {
                SaleCreationLock.Release();
            }
        }

        private async Task<SaleDto> CreateOnceAsync(CreateSaleDto dto, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellation);

            var product = dto.Items
                .Select(x => x.ProductId)
                .Distinct()
                .ToList();

            var products = await db.Products
                .Where(x => product.Contains(x.Id) && x.IsActive)
                .ToDictionaryAsync(x => x.Id, cancellation);

            if (products.Count() != product.Count)
                throw new Exception("Savatdagi mahsulotlardan biri topilmadi yoki faol emas.");

            var now = DateTime.UtcNow;
            var items = new List<SaleItem>();

            foreach (var line in dto.Items)
            {
                var p = products[line.ProductId];
                items.Add(new SaleItem
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    UnitPrice = p.Price,
                    Quantity = line.Quantity,
                    LineTotal = SaleCalculator.RowSum(p.Price, line.Quantity),
                    CreatedAt = now,
                });
            }

            var sale = new Sale
            {
                Number = await NextNumberAsync(db, cancellation),
                PaymentMethod = dto.PaymentMethod,
                SaleStatus = SaleStatus.Completed,
                TotalAmount = SaleCalculator.TotolSale(items.Select(x => x.LineTotal)),
                CreatedAt = now,
                SaleItems = items
            };

            await db.Sales.AddAsync(sale, cancellation);
            await db.SaveChangesAsync(cancellation);
            await transaction.CommitAsync(cancellation);

            return new SaleDto
            {
                Id = sale.Id,
                Number = sale.Number,
                TotalAmount = sale.TotalAmount,
                PaymentMethod = sale.PaymentMethod,
            };
        }

        private static bool IsDatabaseLocked(Exception exception)
        {
            for (Exception? current = exception; current != null; current = current.InnerException)
            {
                if (current is SqliteException sqliteException
                    && sqliteException.SqliteErrorCode is 5 or 6)
                    return true;
            }

            return false;
        }

        public async Task<List<SaleDto>> GetTodayAsync(CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var startUtc = DateTime.Now.Date.ToUniversalTime();   // mahalliy "bugun" boshlanishi

            var sales = await db.Sales
                .AsNoTracking()
                .Where(s => s.CreatedAt >= startUtc)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync(cancellation);

            return sales
                .Select(s => new SaleDto
                {
                    Id = s.Id,
                    Number = s.Number,
                    TotalAmount = s.TotalAmount,
                    PaymentMethod = s.PaymentMethod,
                }).ToList();
        }

        public async Task CancelAsync(int saleId, string? reason, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var sale = await db.Sales
                .FirstOrDefaultAsync(s => s.Id == saleId, cancellation) ?? throw new InvalidOperationException("Sotuv topilmadi");

            if (sale.SaleStatus == SaleStatus.Cancelled)
                throw new InvalidOperationException("Sotuv allaqachon bekor qilingan");

            var now = DateTime.UtcNow;
            sale.SaleStatus = SaleStatus.Cancelled;
            sale.CancelledAt = now;
            sale.CancelReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
            sale.UpdatedAt = now;

            await db.SaveChangesAsync(cancellation);
        }
        //20261005-0001
        private static async Task<string> NextNumberAsync(AppDbContext db, CancellationToken cancellation)
        {
            var startUtc = DateTime.Now.Date.ToUniversalTime();
            var countToday = await db.Sales.CountAsync(s => s.CreatedAt >= startUtc, cancellation);
            return $"{DateTime.Now:yyyyMMdd}-{countToday + 1:0000}";
        }
    }
}
