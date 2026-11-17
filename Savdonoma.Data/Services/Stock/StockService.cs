using Savdonoma.Services;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Data.Services.Pricing;

namespace Savdonoma.Data.Services.Stock
{
    public interface IStockService
    {
        Task<StockDocumentDto> CreateDraftAsync(StockDocumentType type, StockDocumentInput input, CancellationToken cancellation);
        Task<StockDocumentDto> UpdateDraftAsync(int documentId, StockDocumentInput input, CancellationToken cancellation);
        Task DeleteDraftAsync(int documentId, CancellationToken cancellation);

        // Qoralamani o'tkazadi: qoldiq va tannarx o'zgaradi
        Task<StockDocumentDto> PostAsync(int documentId, CancellationToken cancellation);

        // O'tkazilgan hujjatni bekor qiladi (faqat rahbar): teskari harakatlar yoziladi
        Task<StockDocumentDto> CancelAsync(int documentId, string reason, CancellationToken cancellation);

        Task<StockDocumentDto> GetAsync(int documentId, CancellationToken cancellation);
        Task<List<StockDocumentListItemDto>> GetListAsync(StockDocumentFilter filter, CancellationToken cancellation);
        Task<List<StockMovementDto>> GetProductHistoryAsync(int productId, CancellationToken cancellation, int take = 500);
        Task<List<LowStockDto>> GetLowStockAsync(CancellationToken cancellation);
    }

    public class StockService : IStockService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;
        private readonly ISystemSettingsService _settings;
        private readonly ILicenseService _license;

        public StockService(
            IDbContextFactory<AppDbContext> factory,
            CurrentUserService currentUser,
            ISystemSettingsService settings,
            ILicenseService license)
        {
            _factory = factory;
            _currentUser = currentUser;
            _settings = settings;
            _license = license;
        }

        // ---------------- Qoralama ----------------

        public async Task<StockDocumentDto> CreateDraftAsync(StockDocumentType type, StockDocumentInput input, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();
            if (!Enum.IsDefined(type))
                throw new InvalidOperationException("Hujjat turi noto'g'ri.");

            await StockLedger.Lock.WaitAsync(cancellation);
            try
            {
                await using var db = await _factory.CreateDbContextAsync(cancellation);
                var now = DateTime.UtcNow;

                var document = new StockDocument
                {
                    Type = type,
                    Status = StockDocumentStatus.Draft,
                    Number = await NextNumberAsync(db, type, cancellation),
                    CreatedAt = now,
                    CreatedByUserId = _currentUser.UserId,
                };

                await FillDraftAsync(db, document, input, cancellation);
                db.StockDocuments.Add(document);
                await db.SaveChangesAsync(cancellation);

                return await LoadDtoAsync(db, document.Id, cancellation);
            }
            finally
            {
                StockLedger.Lock.Release();
            }
        }

        public async Task<StockDocumentDto> UpdateDraftAsync(int documentId, StockDocumentInput input, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var document = await db.StockDocuments
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == documentId, cancellation)
                ?? throw new InvalidOperationException("Hujjat topilmadi.");

            EnsureDraft(document);

            db.StockDocumentLines.RemoveRange(document.Lines);
            document.Lines.Clear();
            await FillDraftAsync(db, document, input, cancellation);
            document.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(cancellation);
            return await LoadDtoAsync(db, document.Id, cancellation);
        }

        public async Task DeleteDraftAsync(int documentId, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var document = await db.StockDocuments
                .Include(x => x.Lines)
                .SingleOrDefaultAsync(x => x.Id == documentId, cancellation)
                ?? throw new InvalidOperationException("Hujjat topilmadi.");

            EnsureDraft(document);

            db.StockDocuments.Remove(document);
            await db.SaveChangesAsync(cancellation);
        }

        // ---------------- O'tkazish ----------------

        public async Task<StockDocumentDto> PostAsync(int documentId, CancellationToken cancellation)
        {
            EnsureCanManage();
            _license.EnsureCanWrite();
            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation) ?? false;

            await StockLedger.Lock.WaitAsync(cancellation);
            try
            {
                await using var db = await _factory.CreateDbContextAsync(cancellation);
                await using var transaction = await db.Database.BeginTransactionAsync(cancellation);

                var document = await db.StockDocuments
                    .Include(x => x.Lines).ThenInclude(x => x.Product)
                    .Include(x => x.Supplier)
                    .SingleOrDefaultAsync(x => x.Id == documentId, cancellation)
                    ?? throw new InvalidOperationException("Hujjat topilmadi.");

                EnsureDraft(document);
                if (document.Lines.Count == 0)
                    throw new InvalidOperationException("Hujjatda birorta ham mahsulot yo'q.");

                var now = DateTime.UtcNow;
                var userId = _currentUser.UserId;

                switch (document.Type)
                {
                    case StockDocumentType.Receipt:
                        PostReceipt(db, document, now, userId);
                        break;
                    case StockDocumentType.WriteOff:
                        PostWriteOff(db, document, now, userId, inventoryEnabled);
                        break;
                    case StockDocumentType.Inventory:
                        PostInventory(db, document, now, userId);
                        break;
                }

                document.Status = StockDocumentStatus.Posted;
                document.PostedAt = now;
                document.PostedByUserId = userId;
                document.UpdatedAt = now;

                await db.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);

                return await LoadDtoAsync(db, document.Id, cancellation);
            }
            finally
            {
                StockLedger.Lock.Release();
            }
        }

        private static void PostReceipt(AppDbContext db, StockDocument document, DateTime now, int? userId)
        {
            foreach (var line in document.Lines)
            {
                var product = line.Product;

                // Tannarx qoldiq o'zgarishidan OLDIN hisoblanadi
                product.CostPrice = CostCalculator.AfterReceipt(product.Quantity, product.CostPrice, line.Quantity, line.CostPrice);

                if (line.NewSalePrice is > 0)
                    product.Price = line.NewSalePrice.Value;

                var movement = StockLedger.Apply(db, product, line.Quantity, StockMovementType.Receipt, now, userId, document);
                movement.CostPrice = line.CostPrice; // harakatda kirim narxi saqlanadi
            }

            document.TotalCost = ReceiptTotal(document.Lines);

            if (document.Supplier is not null)
            {
                document.Supplier.Debt += document.TotalCost;
                document.Supplier.UpdatedAt = now;
            }
        }

        private static void PostWriteOff(AppDbContext db, StockDocument document, DateTime now, int? userId, bool inventoryEnabled)
        {
            foreach (var line in document.Lines)
            {
                var product = line.Product;
                if (inventoryEnabled && product.Quantity < line.Quantity)
                    throw new InvalidOperationException(
                        $"'{product.Name}' qoldig'i yetarli emas. Mavjud: {product.Quantity:0.###}, chiqarilmoqda: {line.Quantity:0.###}.");

                line.CostPrice = product.CostPrice;
                StockLedger.Apply(db, product, -line.Quantity, StockMovementType.WriteOff, now, userId, document, note: document.Note);
            }

            document.TotalCost = ReceiptTotal(document.Lines);
        }

        private static void PostInventory(AppDbContext db, StockDocument document, DateTime now, int? userId)
        {
            long total = 0;
            foreach (var line in document.Lines)
            {
                var product = line.Product;
                line.ExpectedQuantity = product.Quantity;
                line.CostPrice = product.CostPrice;

                var difference = line.Quantity - product.Quantity;
                if (difference != 0)
                {
                    StockLedger.Apply(db, product, difference, StockMovementType.InventoryAdjustment, now, userId, document);
                    total += SaleCalculator.RowSum(line.CostPrice, difference);
                }
            }

            // Inventarizatsiyada: ortiqcha (+) va kamomad (-) ning tannarx bo'yicha sof qiymati
            document.TotalCost = total;
        }

        // ---------------- Bekor qilish ----------------

        public async Task<StockDocumentDto> CancelAsync(int documentId, string reason, CancellationToken cancellation)
        {
            if (!_currentUser.IsOwner)
                throw new UnauthorizedAccessException("O'tkazilgan hujjatni faqat rahbar bekor qila oladi.");
            _license.EnsureCanWrite();
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("Bekor qilish sababini kiriting.");

            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation) ?? false;

            await StockLedger.Lock.WaitAsync(cancellation);
            try
            {
                await using var db = await _factory.CreateDbContextAsync(cancellation);
                await using var transaction = await db.Database.BeginTransactionAsync(cancellation);

                var document = await db.StockDocuments
                    .Include(x => x.Lines).ThenInclude(x => x.Product)
                    .Include(x => x.Supplier)
                    .SingleOrDefaultAsync(x => x.Id == documentId, cancellation)
                    ?? throw new InvalidOperationException("Hujjat topilmadi.");

                if (document.Status != StockDocumentStatus.Posted)
                    throw new InvalidOperationException(document.Status == StockDocumentStatus.Cancelled
                        ? "Hujjat allaqachon bekor qilingan."
                        : "Faqat o'tkazilgan hujjatni bekor qilish mumkin. Qoralamani o'chirish mumkin.");

                var now = DateTime.UtcNow;
                var userId = _currentUser.UserId;
                var note = $"{document.Number} bekor qilindi: {reason.Trim()}";

                foreach (var line in document.Lines)
                {
                    var product = line.Product;
                    var reverse = document.Type switch
                    {
                        StockDocumentType.Receipt => -line.Quantity,
                        StockDocumentType.WriteOff => line.Quantity,
                        StockDocumentType.Inventory => -(line.Quantity - (line.ExpectedQuantity ?? line.Quantity)),
                        _ => 0m
                    };

                    if (reverse == 0)
                        continue;

                    if (inventoryEnabled && product.Quantity + reverse < 0)
                        throw new InvalidOperationException(
                            $"'{product.Name}' qoldig'i yetarli emas: hujjatni bekor qilsak qoldiq manfiy bo'ladi " +
                            $"(mavjud: {product.Quantity:0.###}). Ehtimol, bu tovar allaqachon sotilgan.");

                    if (document.Type == StockDocumentType.Receipt)
                        product.CostPrice = CostCalculator.AfterReceiptCancel(product.Quantity, product.CostPrice, line.Quantity, line.CostPrice);

                    StockLedger.Apply(db, product, reverse, StockMovementType.DocumentCancel, now, userId, document, note: note);
                }

                if (document.Type == StockDocumentType.Receipt && document.Supplier is not null)
                {
                    document.Supplier.Debt -= document.TotalCost;
                    document.Supplier.UpdatedAt = now;
                }

                document.Status = StockDocumentStatus.Cancelled;
                document.CancelledAt = now;
                document.CancelledByUserId = userId;
                document.CancelReason = reason.Trim();
                document.UpdatedAt = now;

                await db.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);

                return await LoadDtoAsync(db, document.Id, cancellation);
            }
            finally
            {
                StockLedger.Lock.Release();
            }
        }

        // ---------------- O'qish ----------------

        public async Task<StockDocumentDto> GetAsync(int documentId, CancellationToken cancellation)
        {
            EnsureCanManage();
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            return await LoadDtoAsync(db, documentId, cancellation);
        }

        public async Task<List<StockDocumentListItemDto>> GetListAsync(StockDocumentFilter filter, CancellationToken cancellation)
        {
            EnsureCanManage();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var query = db.StockDocuments.AsNoTracking().AsQueryable();
            if (filter.Type is not null)
                query = query.Where(x => x.Type == filter.Type);
            if (filter.Status is not null)
                query = query.Where(x => x.Status == filter.Status);
            if (filter.SupplierId is not null)
                query = query.Where(x => x.SupplierId == filter.SupplierId);
            if (filter.From is not null)
            {
                var fromUtc = filter.From.Value.Date.ToUniversalTime();
                query = query.Where(x => x.CreatedAt >= fromUtc);
            }
            if (filter.To is not null)
            {
                var toUtc = filter.To.Value.Date.AddDays(1).ToUniversalTime();
                query = query.Where(x => x.CreatedAt < toUtc);
            }

            var rows = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new StockDocumentListItemDto
                {
                    Id = x.Id,
                    Number = x.Number,
                    Type = x.Type,
                    Status = x.Status,
                    SupplierName = x.Supplier != null ? x.Supplier.Name : null,
                    TotalCost = x.TotalCost,
                    LineCount = x.Lines.Count,
                    CreatedAt = x.CreatedAt,
                    PostedAt = x.PostedAt,
                })
                .ToListAsync(cancellation);

            foreach (var row in rows)
            {
                row.CreatedAt = row.CreatedAt.ToLocalTime();
                row.PostedAt = row.PostedAt?.ToLocalTime();
            }

            return rows;
        }

        public async Task<List<StockMovementDto>> GetProductHistoryAsync(int productId, CancellationToken cancellation, int take = 500)
        {
            EnsureCanManage();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var rows = await db.StockMovements
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .OrderByDescending(x => x.Id)
                .Take(take)
                .Select(x => new StockMovementDto
                {
                    Id = x.Id,
                    Type = x.Type,
                    Quantity = x.Quantity,
                    BalanceAfter = x.BalanceAfter,
                    CostPrice = x.CostPrice,
                    CreatedAt = x.CreatedAt,
                    DocumentNumber = x.Document != null ? x.Document.Number : null,
                    SaleNumber = x.Sale != null ? x.Sale.Number : null,
                    Note = x.Note,
                })
                .ToListAsync(cancellation);

            foreach (var row in rows)
                row.CreatedAt = row.CreatedAt.ToLocalTime();

            return rows;
        }

        public async Task<List<LowStockDto>> GetLowStockAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            // SQLite decimal'ni to'g'ri solishtira olmaydi, shuning uchun solishtirish xotirada
            var candidates = await db.Products
                .AsNoTracking()
                .Where(x => x.IsActive && x.MinQuantity != null)
                .Select(x => new LowStockDto
                {
                    ProductId = x.Id,
                    Name = x.Name,
                    Unit = x.Unit,
                    Quantity = x.Quantity,
                    MinQuantity = x.MinQuantity!.Value,
                })
                .ToListAsync(cancellation);

            return candidates
                .Where(x => x.Quantity <= x.MinQuantity)
                .OrderBy(x => x.Quantity - x.MinQuantity)
                .ThenBy(x => x.Name)
                .ToList();
        }

        // ---------------- Yordamchilar ----------------

        private async Task FillDraftAsync(AppDbContext db, StockDocument document, StockDocumentInput input, CancellationToken cancellation)
        {
            if (input.Lines.Count == 0)
                throw new InvalidOperationException("Kamida bitta mahsulot qo'shing.");

            var duplicate = input.Lines.GroupBy(x => x.ProductId).FirstOrDefault(g => g.Count() > 1);
            if (duplicate is not null)
                throw new InvalidOperationException("Bitta mahsulot hujjatda ikki marta bo'lishi mumkin emas. Miqdorini birlashtiring.");

            var isReceipt = document.Type == StockDocumentType.Receipt;
            var isInventory = document.Type == StockDocumentType.Inventory;

            if (input.SupplierId is not null)
            {
                if (!isReceipt)
                    throw new InvalidOperationException("Yetkazib beruvchi faqat kirim hujjatida ko'rsatiladi.");

                var supplierActive = await db.Suppliers.AnyAsync(x => x.Id == input.SupplierId && x.IsActive, cancellation);
                if (!supplierActive)
                    throw new InvalidOperationException("Yetkazib beruvchi topilmadi yoki nofaol.");
            }

            var productIds = input.Lines.Select(x => x.ProductId).ToList();
            var products = await db.Products
                .Where(x => productIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, cancellation);

            // Kirimda sotuv narxi kiritilmagan bo'lsa: yangi kirim narxi + ustama (1-usul)
            var pricing = isReceipt ? await PricingService.LoadRulesAsync(db, cancellation) : null;

            foreach (var line in input.Lines)
            {
                if (!products.TryGetValue(line.ProductId, out var product) || !product.IsActive)
                    throw new InvalidOperationException("Mahsulotlardan biri topilmadi yoki faol emas.");

                if (isInventory ? line.Quantity < 0 : line.Quantity <= 0)
                    throw new InvalidOperationException($"'{product.Name}': miqdor noto'g'ri.");

                if (isReceipt && line.CostPrice < 0)
                    throw new InvalidOperationException($"'{product.Name}': tannarx manfiy bo'lishi mumkin emas.");

                if (line.NewSalePrice is not null && (!isReceipt || line.NewSalePrice <= 0))
                    throw new InvalidOperationException($"'{product.Name}': yangi sotuv narxi noto'g'ri.");

                document.Lines.Add(new StockDocumentLine
                {
                    ProductId = product.Id,
                    Quantity = line.Quantity,
                    // Kirimda foydalanuvchi kiritgan narx, boshqalarda joriy tannarx (o'tkazishda yangilanadi)
                    CostPrice = isReceipt ? line.CostPrice : product.CostPrice,
                    NewSalePrice = isReceipt
                        ? line.NewSalePrice ?? pricing!.SuggestSalePrice(line.CostPrice, product.MarkupPercent, product.CategoryId)
                        : null,
                });
            }

            document.SupplierId = isReceipt ? input.SupplierId : null;
            document.Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
            document.TotalCost = isInventory ? 0 : ReceiptTotal(document.Lines);
        }

        private static long ReceiptTotal(IEnumerable<StockDocumentLine> lines)
            => lines.Sum(x => SaleCalculator.RowSum(x.CostPrice, x.Quantity));

        private static async Task<string> NextNumberAsync(AppDbContext db, StockDocumentType type, CancellationToken cancellation)
        {
            var prefix = type switch
            {
                StockDocumentType.Receipt => "KR",
                StockDocumentType.WriteOff => "HCH",
                StockDocumentType.Inventory => "INV",
                _ => "DOC"
            };

            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var start = $"{prefix}-{datePart}-";
            var existing = await db.StockDocuments
                .Where(x => x.Number.StartsWith(start))
                .Select(x => x.Number)
                .ToListAsync(cancellation);

            var next = existing
                .Select(n => int.TryParse(n[start.Length..], out var v) ? v : 0)
                .DefaultIfEmpty(0)
                .Max() + 1;

            return $"{start}{next:0000}";
        }

        private static async Task<StockDocumentDto> LoadDtoAsync(AppDbContext db, int documentId, CancellationToken cancellation)
        {
            var document = await db.StockDocuments
                .AsNoTracking()
                .Include(x => x.Supplier)
                .Include(x => x.Lines).ThenInclude(x => x.Product)
                .SingleOrDefaultAsync(x => x.Id == documentId, cancellation)
                ?? throw new InvalidOperationException("Hujjat topilmadi.");

            var isInventory = document.Type == StockDocumentType.Inventory;

            return new StockDocumentDto
            {
                Id = document.Id,
                Number = document.Number,
                Type = document.Type,
                Status = document.Status,
                SupplierId = document.SupplierId,
                SupplierName = document.Supplier?.Name,
                Note = document.Note,
                TotalCost = document.TotalCost,
                CreatedAt = document.CreatedAt.ToLocalTime(),
                PostedAt = document.PostedAt?.ToLocalTime(),
                CancelledAt = document.CancelledAt?.ToLocalTime(),
                CancelReason = document.CancelReason,
                Lines = document.Lines
                    .OrderBy(x => x.Id)
                    .Select(x => new StockDocumentLineDto
                    {
                        Id = x.Id,
                        ProductId = x.ProductId,
                        ProductName = x.Product.Name,
                        Unit = x.Product.Unit,
                        Quantity = x.Quantity,
                        CostPrice = x.CostPrice,
                        LineTotal = SaleCalculator.RowSum(x.CostPrice, x.Quantity),
                        NewSalePrice = x.NewSalePrice,
                        CurrentSalePrice = x.Product.Price,
                        ExpectedQuantity = isInventory
                            ? (document.Status == StockDocumentStatus.Draft ? x.Product.Quantity : x.ExpectedQuantity)
                            : null,
                    })
                    .ToList(),
            };
        }

        private static void EnsureDraft(StockDocument document)
        {
            if (document.Status != StockDocumentStatus.Draft)
                throw new InvalidOperationException("O'tkazilgan yoki bekor qilingan hujjatni o'zgartirib bo'lmaydi.");
        }

        private void EnsureCanManage()
        {
            if (!_currentUser.CanManageStock)
                throw new UnauthorizedAccessException("Ombor amallari faqat rahbar va menejer uchun ruxsat etilgan.");
        }
    }
}
