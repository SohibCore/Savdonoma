using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Microsoft.Data.Sqlite;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.SystemSetting;

namespace Savdonoma.Data.Services.Sales
{
    public class SaleService : ISaleService
    {
        private static readonly SemaphoreSlim SaleCreationLock = new(1, 1);
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ISystemSettingsService _settings;
        private readonly ILicenseService _license;
        public SaleService(IDbContextFactory<AppDbContext> factory, ISystemSettingsService system, ILicenseService license)
        {
            _factory = factory;
            _settings = system;
            _license = license;
        }

        public async Task<SaleDto> CreateAsync(CreateSaleDto dto, CancellationToken cancellation)
        {
            _license.EnsureCanWrite();

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
            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation);

            if (inventoryEnabled is null)
            {
                throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");
            }

            await using var db = await _factory.CreateDbContextAsync(cancellation);
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

            if (inventoryEnabled.Value)
            {
                var requestedQuantities = dto.Items
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(group => group.Key, group => group.Sum(x => x.Quantity));

                foreach (var request in requestedQuantities)
                {
                    var p = products[request.Key];
                    var requestedQuantity = request.Value;

                    if (p.Quantity < requestedQuantity)
                    {
                        throw new InvalidOperationException(
                            $"'{p.Name}' mahsulotidan qoldiq yetarli emas. " +
                            $"Mavjud: {p.Quantity}, " +
                            $"so'ralgan: {requestedQuantity}.");
                    }

                    p.Quantity -= requestedQuantity;
                }
            }

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
                    Unit = p.Unit,
                    LineTotal = SaleCalculator.RowSum(p.Price, line.Quantity),
                    CreatedAt = now,
                });
            }
            var sale = new Sale
            {
                Number = await NextNumberAsync(db, cancellation),
                PaymentMethod = dto.PaymentMethod,
                SaleStatus = SaleStatus.Completed,
                TotalAmount = SaleCalculator.TotalSale(items.Select(x => x.LineTotal)),
                CreatedAt = now,
                UserId = dto.UserId,
                SaleItems = items,
                ReceivedAmount = dto.ReceivedAmount,
                ChangeAmount = dto.ChangeAmount,
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

        public async Task<ReceiptModel> GetReceipt(int saleId, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var sale = await db.Sales
                .Include(s => s.SaleItems)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == saleId, cancellation);

            if (sale == null)
                throw new Exception("Sotuv topilmadi");

            var shopName = sale.User?.OrgName;
            var receipt = new ReceiptModel
            {
                ReceiptNumber = sale.Id,
                Date = sale.CreatedAt,
                Cashier = sale.User?.FullName ?? "Noma'lum sotuvchi",
                PaymentType = sale.PaymentMethod,
                Paid = sale.SaleItems.Sum(i => i.Quantity * i.UnitPrice), // vaqtincha = jami

                ShopName = string.IsNullOrWhiteSpace(shopName)
                    ? "Noma'lum do'kon"
                    : shopName.Trim(),

                Items = sale.SaleItems.Select(i => new ReceiptItem
                {
                    Name = i.ProductName,
                    Quantity = i.Quantity,
                    Price = i.UnitPrice
                }).ToList()
            };

            return receipt;
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
            await using var db = await _factory.CreateDbContextAsync(cancellation);

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
            _license.EnsureCanWrite();
            await SaleCreationLock.WaitAsync(cancellation);

            try
            {
                var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation);

                if (inventoryEnabled is null)
                    throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

                await using var db = await _factory.CreateDbContextAsync(cancellation);

                await using var transaction = await db.Database.BeginTransactionAsync(cancellation);

                var sale = await db.Sales
                    .Include(s => s.SaleItems)
                    .ThenInclude(item => item.Product)
                    .FirstOrDefaultAsync(s => s.Id == saleId, cancellation);

                if (sale is null)
                    throw new InvalidOperationException("Sotuv topilmadi.");

                if (sale.SaleStatus == SaleStatus.Cancelled)
                    throw new InvalidOperationException("Sotuv allaqachon bekor qilingan.");

                var now = DateTime.UtcNow;

                // Qoldiq yoqilgan bo'lsa, mahsulotlarni qayta omborga qo'shamiz.
                if (inventoryEnabled.Value)
                {
                    foreach (var item in sale.SaleItems)
                    {
                        item.Product.Quantity += item.Quantity;
                        item.Product.UpdatedAt = now;
                    }
                }

                sale.SaleStatus = SaleStatus.Cancelled;
                sale.CancelledAt = now;
                sale.CancelReason = string.IsNullOrWhiteSpace(reason)
                    ? null
                    : reason.Trim();
                sale.UpdatedAt = now;

                await db.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);
            }
            finally
            {
                SaleCreationLock.Release();
            }
        }

        public async Task<List<SaleDetailDto>> GetDetailAsync(int saleId, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            return await db.Sales
                .AsNoTracking()
                .Where(x => x.Id == saleId)
                .Select(x => new SaleDetailDto
                {
                    Number = x.Number,
                    UserId = x.UserId,
                    SellerName = x.User != null ? x.User.FullName : "Noma'lum sotuvchi",
                    TotalAmount = x.TotalAmount,
                    ChangeAmount = x.ChangeAmount,
                    PaymentMethod = x.PaymentMethod,
                    ReceivedAmount = x.ReceivedAmount,
                    Items = x.SaleItems.Select(i => new SaleItem
                    {
                        ProductName = i.ProductName,
                        UnitPrice = i.UnitPrice,
                        Quantity = i.Quantity,
                        LineTotal = i.LineTotal,
                        Unit = i.Unit,
                    }).ToList()
                }).ToListAsync(cancellation);
        }
        //20261005-0001
        private static async Task<string> NextNumberAsync(AppDbContext db, CancellationToken cancellation)
        {
            var startUtc = DateTime.Now.Date.ToUniversalTime();
            var countToday = await db.Sales.CountAsync(s => s.CreatedAt >= startUtc, cancellation);
            return $"{DateTime.Now:yyyyMMdd}-{countToday + 1:000000}";
        }
    }
}
