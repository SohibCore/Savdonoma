using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Microsoft.Data.Sqlite;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Services;

namespace Savdonoma.Data.Services.Sales
{
    public class SaleService : ISaleService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ISystemSettingsService _settings;
        private readonly ILicenseService _license;
        private readonly CurrentUserService _currentUser;
        public SaleService(IDbContextFactory<AppDbContext> factory, ISystemSettingsService system, ILicenseService license, CurrentUserService currentUser)
        {
            _factory = factory;
            _settings = system;
            _license = license;
            _currentUser = currentUser;
        }

        public async Task<SaleDto> CreateAsync(CreateSaleDto dto, CancellationToken cancellation)
        {
            _license.EnsureCanWrite();

            if (dto.Items.Count == 0)
                throw new Exception("Savat bo'sh.");

            if (dto.Items.Any(q => q.Quantity <= 0))
                throw new Exception("Miqdor 0 dan katta bo'lilshi kerak.");

            await StockLedger.Lock.WaitAsync(cancellation);
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
                StockLedger.Lock.Release();
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
                    CostPrice = p.CostPrice,
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

            // Qoldiq ikkala rejimda ham jurnal orqali kamayadi.
            // Qoldiqsiz rejimda faqat yetishmovchilik tekshirilmaydi (yuqorida).
            foreach (var line in dto.Items)
                StockLedger.Apply(db, products[line.ProductId], -line.Quantity, StockMovementType.Sale, now, dto.UserId, sale: sale);

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
                     .ThenInclude(i => i.Product)
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == saleId, cancellation) ?? throw new Exception("Sotuv topilmadi");

            var organization = await db.Organizations
                .AsNoTracking()
                .Where(x => x.Id == sale.User!.OrgId)
                .Select(x => new { x.Address, x.INN })
                .SingleOrDefaultAsync(cancellation);

            var shopName = sale.User?.OrgName;
            var receipt = new ReceiptModel
            {
                ShopName = string.IsNullOrWhiteSpace(shopName) ? "Noma'lum do'kon" : shopName.Trim(),
                ReceiptNumber = sale.Id,
                Address = organization?.Address ?? "No'malum manzil",
                INN = organization?.INN ?? "No'malum INN",
                Cashier = sale.User?.FullName ?? "Noma'lum sotuvchi",
                Date = sale.CreatedAt,
                PaymentType = sale.PaymentMethod,
                Paid = sale.ReceivedAmount ?? sale.TotalAmount,
                Cash = sale.PaymentMethod == PaymentMethod.Naqd ? sale.TotalAmount : 0,
                Card = sale.PaymentMethod == PaymentMethod.Karta ? sale.TotalAmount : 0,
            };

            receipt.Items = sale.SaleItems.Select(i => new ReceiptItem
            {
                Name = i.ProductName,
                Quantity = i.Quantity,
                Price = i.UnitPrice,
                Unit = i.Unit,
                VatRate = receipt.VatRate,
                Mxik = i.Product?.MxikCode,
                Barcode = i.Product?.Barcode,
            }).ToList();

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

            var sales = await VisibleSales(db)
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

        // Sotuvchi faqat o'z sotuvlarini ko'radi, rahbar va menejer hammasini
        private IQueryable<Sale> VisibleSales(AppDbContext db)
        {
            var sales = db.Sales.AsNoTracking();
            if (_currentUser.CanViewAllSales)
                return sales;

            var userId = _currentUser.UserId ?? -1;
            return sales.Where(s => s.UserId == userId);
        }

        public async Task CancelAsync(int saleId, string? reason, CancellationToken cancellation)
        {
            if (!_currentUser.CanCancelSales)
                throw new UnauthorizedAccessException("Chekni faqat rahbar yoki menejer bekor qila oladi.");

            _license.EnsureCanWrite();
            await StockLedger.Lock.WaitAsync(cancellation);

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

                // Jurnalda sotuv harakati bor bo'lsa, qoldiq doim qaytariladi.
                // Jurnalgacha qilingan eski sotuvlarda esa qoldiq faqat qoldiq rejimida kamaygan edi.
                var hasMovements = await db.StockMovements
                    .AnyAsync(m => m.SaleId == sale.Id && m.Type == StockMovementType.Sale, cancellation);

                if (hasMovements || inventoryEnabled.Value)
                {
                    foreach (var item in sale.SaleItems)
                        StockLedger.Apply(db, item.Product, item.Quantity, StockMovementType.SaleCancel, now,
                            _currentUser.UserId, sale: sale, note: sale.CancelReason ?? reason?.Trim());
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
                StockLedger.Lock.Release();
            }
        }

        public async Task<List<SaleDetailDto>> GetDetailAsync(int saleId, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            return await VisibleSales(db)
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
