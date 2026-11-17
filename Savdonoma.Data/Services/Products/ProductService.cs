using Savdonoma.Services;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Data.Services.SystemSetting;

namespace Savdonoma.Data.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ISystemSettingsService _settings;
        private readonly CurrentUserService _currentUser;
        private readonly ILicenseService _license;

        public ProductService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser, ISystemSettingsService system, ILicenseService license)
        {
            _factory = factory;
            _currentUser = currentUser;
            _settings = system;
            _license = license;
        }

        public async Task<List<ProductDto>> GetListAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var products = await db.Products
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellation);

            return products.Select(ReturnDto).ToList();
        }

        public async Task<ProductDto> GetAsync(int Id, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var product = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Id, cancellation) ?? throw new Exception("Mahsulot topilmadi.");

            return ReturnDto(product);
        }

        public async Task<List<ProductDto>> SearchAsync(string? text, CancellationToken cancellation, int take = 50)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var product = db.Products
                .AsNoTracking()
                .Where(x => x.IsActive);

            var key = NameNormalizer.Normalize(text ?? string.Empty);
            var trimmedText = text?.Trim();
            var barcode = trimmedText;

            if (key != "")
            {
                var escapedText = (trimmedText ?? "")
                    .Replace("\\", "\\\\")
                    .Replace("%", "\\%")
                    .Replace("_", "\\_");
                var namePattern = $"%{escapedText}%";

                product = product.Where(x =>
                    x.NameSearch.Contains(key)
                    || EF.Functions.Like(x.Name, namePattern, "\\")
                    || x.Barcode == barcode
                    || x.Barcodes.Any(b => b.Barcode == barcode));
            }

            var items = await product
                .OrderBy(x => x.NameSearch)
                .Take(take).ToListAsync(cancellation);

            return items.Select(ReturnDto).ToList();
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            ValidateCreating(dto);

            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation);

            if (inventoryEnabled is null)
                throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

            if (inventoryEnabled.Value && dto.Quantity is < 0)
                throw new ArgumentException("Mahsulot qoldig'i manfiy bo'lishi mumkin emas.");

            var barcode = CleanBarcode(dto.Barcode);
            var scaleCode = CleanScaleCode(dto.ScaleCode);
            var quantity = inventoryEnabled.Value ? dto.Quantity : 0m;

            await StockLedger.Lock.WaitAsync(cancellation);
            try
            {
                await using var db = await _factory.CreateDbContextAsync(cancellation);
                if (barcode is not null)
                    await EnsureBarcodeFreeAsync(db, barcode, exceptProductId: null, cancellation);
                if (scaleCode is not null)
                    await EnsureScaleCodeFreeAsync(db, scaleCode, exceptProductId: null, cancellation);

                var now = DateTime.UtcNow;
                var product = new Product()
                {
                    Name = dto.Name.Trim(),
                    NameSearch = NameNormalizer.Normalize(dto.Name),
                    Unit = dto.Unit,
                    Price = dto.Price,
                    CategoryId = dto.CategoryId > 0 ? dto.CategoryId : null,
                    Barcode = barcode,
                    IsActive = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                    Quantity = 0,
                    CostPrice = dto.CostPrice ?? 0,
                    MarkupPercent = dto.MarkupPercent,
                    MinQuantity = dto.MinQuantity,
                    ScaleCode = scaleCode,
                    MxikCode = Clean(dto.MxikCode),
                    PackageCode = Clean(dto.PackageCode),
                    VatPercent = dto.VatPercent,
                    IsMarked = dto.IsMarked,
                };

                db.Products.Add(product);

                // Boshlang'ich qoldiq jurnalga yoziladi
                if (quantity != 0)
                    StockLedger.Apply(db, product, quantity, StockMovementType.OpeningBalance, now, _currentUser.UserId);

                await db.SaveChangesAsync(cancellation);
                return ReturnDto(product);
            }
            finally
            {
                StockLedger.Lock.Release();
            }
        }

        public async Task DeactivateAsync(int Id, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == Id, cancellation) ?? throw new Exception("Mahsulot topilmadi");

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
        }

        public async Task<ProductDto> UpdateAsync(UpdateProductDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            var price = ValidateUpdating(dto);
            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation);
            if (inventoryEnabled is null)
                throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

            if (inventoryEnabled.Value && dto.Quantity < 0)
                throw new ArgumentException("Mahsulot qoldig'i manfiy bo'lishi mumkin emas.", nameof(dto.Quantity));

            await StockLedger.Lock.WaitAsync(cancellation);
            try
            {
                await using var db = await _factory.CreateDbContextAsync(cancellation);

                var product = await db.Products.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellation) ?? throw new Exception("Mahsulot topilmadi");
                var now = DateTime.UtcNow;

                product.Price = price;

                if (dto.UpdateDetails)
                {
                    if (dto.CostPrice is < 0)
                        throw new ArgumentException("Tannarx manfiy bo'lishi mumkin emas.", nameof(dto.CostPrice));
                    if (dto.MinQuantity is < 0)
                        throw new ArgumentException("Minimal qoldiq manfiy bo'lishi mumkin emas.", nameof(dto.MinQuantity));
                    PriceCalculator.ValidateMarkup(dto.MarkupPercent);

                    var scaleCode = CleanScaleCode(dto.ScaleCode);
                    if (scaleCode is not null && scaleCode != product.ScaleCode)
                        await EnsureScaleCodeFreeAsync(db, scaleCode, product.Id, cancellation);

                    product.CostPrice = dto.CostPrice ?? product.CostPrice;
                    product.MarkupPercent = dto.MarkupPercent;
                    product.MinQuantity = dto.MinQuantity;
                    product.ScaleCode = scaleCode;
                    product.MxikCode = Clean(dto.MxikCode);
                    product.PackageCode = Clean(dto.PackageCode);
                    product.VatPercent = dto.VatPercent;
                    product.IsMarked = dto.IsMarked;
                }

                // Qoldiqni qo'lda o'zgartirish faqat qoldiq rejimida va jurnal orqali.
                // Qoldiqsiz rejimda qoldiq o'zgarmaydi (avval 0 ga tushirilardi).
                if (inventoryEnabled.Value && dto.Quantity is not null && dto.Quantity.Value != product.Quantity)
                {
                    StockLedger.Apply(db, product, dto.Quantity.Value - product.Quantity,
                        StockMovementType.ManualAdjustment, now, _currentUser.UserId, note: "Mahsulot kartasida tuzatildi");
                }

                product.UpdatedAt = now;
                await db.SaveChangesAsync(cancellation);
                return ReturnDto(product);
            }
            finally
            {
                StockLedger.Lock.Release();
            }
        }

        private void EnsureOwner()
        {
            if (!_currentUser.IsOwner)
                throw new UnauthorizedAccessException("Mahsulotlarni faqat do‘kon egasi boshqara oladi.");
        }

        public async Task<Product?> FindByBarcodeAsync(string code)
        {
            var barcode = code?.Trim();
            if (string.IsNullOrEmpty(barcode))
                return null;

            await using var db = await _factory.CreateDbContextAsync();

            return await db.Products
                .AsNoTracking()
                .Where(p => p.IsActive && (p.Barcode == barcode || p.Barcodes.Any(b => b.Barcode == barcode)))
                .FirstOrDefaultAsync();
        }

        public async Task<BarcodeLookupResult?> ResolveBarcodeAsync(string code, CancellationToken cancellation)
        {
            // Avval oddiy barcode: do'konning o'z "2..." kodlari tarozi kodi bilan adashmasin
            var product = await FindByBarcodeAsync(code);
            if (product is not null)
                return new BarcodeLookupResult { Product = ReturnDto(product) };

            if (!ScaleBarcode.TryParse(code, out var scaleCode, out var weight))
                return null;

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var weighed = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.IsActive && p.ScaleCode == scaleCode, cancellation);

            return weighed is null
                ? null
                : new BarcodeLookupResult { Product = ReturnDto(weighed), Quantity = weight };
        }

        public async Task<List<string>> GetExtraBarcodesAsync(int productId, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            return await db.ProductBarcodes
                .AsNoTracking()
                .Where(x => x.ProductId == productId)
                .OrderBy(x => x.Id)
                .Select(x => x.Barcode)
                .ToListAsync(cancellation);
        }

        public async Task AddBarcodeAsync(int productId, string barcode, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            ValidateBarcode(barcode);
            var code = CleanBarcode(barcode) ?? throw new ArgumentException("Barcode bo'sh bo'lmasligi kerak.", nameof(barcode));

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == productId, cancellation)
                ?? throw new InvalidOperationException("Mahsulot topilmadi.");

            await EnsureBarcodeFreeAsync(db, code, exceptProductId: null, cancellation);

            db.ProductBarcodes.Add(new ProductBarcode { ProductId = product.Id, Barcode = code, CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(cancellation);
        }

        public async Task RemoveBarcodeAsync(int productId, string barcode, CancellationToken cancellation)
        {
            EnsureOwner();
            _license.EnsureCanWrite();
            var code = CleanBarcode(barcode);

            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var row = await db.ProductBarcodes.FirstOrDefaultAsync(x => x.ProductId == productId && x.Barcode == code, cancellation)
                ?? throw new InvalidOperationException("Bunday qo'shimcha barcode topilmadi.");

            db.ProductBarcodes.Remove(row);
            await db.SaveChangesAsync(cancellation);
        }

        // Barcode asosiy (Product.Barcode) va qo'shimcha (ProductBarcode) jadvallar bo'yicha yagona bo'lishi kerak
        private static async Task EnsureBarcodeFreeAsync(AppDbContext db, string barcode, int? exceptProductId, CancellationToken cancellation)
        {
            var owner = await db.Products
                .AsNoTracking()
                .Where(p => p.Id != exceptProductId && (p.Barcode == barcode || p.Barcodes.Any(b => b.Barcode == barcode)))
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellation);

            if (owner is not null)
                throw new InvalidOperationException($"Bu barcode \"{owner}\" mahsulotiga biriktirilgan.");
        }

        private static async Task EnsureScaleCodeFreeAsync(AppDbContext db, string scaleCode, int? exceptProductId, CancellationToken cancellation)
        {
            var owner = await db.Products
                .AsNoTracking()
                .Where(p => p.Id != exceptProductId && p.ScaleCode == scaleCode)
                .Select(p => p.Name)
                .FirstOrDefaultAsync(cancellation);

            if (owner is not null)
                throw new InvalidOperationException($"Bu tarozi kodi \"{owner}\" mahsulotiga biriktirilgan.");
        }

        // Yordamichi funksiyalar
        private static void ValidateCreating(CreateProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Mahsulot nomi bo'sh bo'lmasligi kerak", nameof(dto.Name));
            }
            if (dto.Price <= 0)
            {
                throw new ArgumentException("Mahsulot narxi 0 dan katta bo'lishi kerak", nameof(dto.Price));
            }
            if (dto.CostPrice is < 0)
            {
                throw new ArgumentException("Tannarx manfiy bo'lishi mumkin emas", nameof(dto.CostPrice));
            }
            if (dto.MinQuantity is < 0)
            {
                throw new ArgumentException("Minimal qoldiq manfiy bo'lishi mumkin emas", nameof(dto.MinQuantity));
            }
            PriceCalculator.ValidateMarkup(dto.MarkupPercent);
            ValidateBarcode(dto.Barcode);
        }

        private static ProductDto ReturnDto(Product p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            NameSearch = p.NameSearch,
            Unit = p.Unit,
            Price = p.Price,
            IsActive = p.IsActive,
            CategoryId = p.CategoryId,
            Barcode = p.Barcode,
            Quantity = p.Quantity,
            CostPrice = p.CostPrice,
            MarkupPercent = p.MarkupPercent,
            MinQuantity = p.MinQuantity,
            ScaleCode = p.ScaleCode,
            MxikCode = p.MxikCode,
            PackageCode = p.PackageCode,
            VatPercent = p.VatPercent,
            IsMarked = p.IsMarked,
        };

        private static string? CleanBarcode(string? barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }
            return barcode.Trim();
        }

        private static string? Clean(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        // Tarozi PLU kodi: 1-5 ta raqam, boshidagi nollarsiz saqlanadi
        private static string? CleanScaleCode(string? scaleCode)
        {
            var code = Clean(scaleCode);
            if (code is null)
                return null;

            if (code.Length > 5 || !code.All(char.IsAsciiDigit))
                throw new ArgumentException("Tarozi kodi 1-5 ta raqamdan iborat bo'lishi kerak.", nameof(scaleCode));

            return ScaleBarcode.NormalizeCode(code);
        }

        private static long ValidateUpdating(UpdateProductDto dto)
        {
            if (!dto.Price.HasValue || dto.Price.Value <= 0)
            {
                throw new ArgumentException("Mahsulot narxi 0 dan katta bo'lishi kerak", nameof(dto.Price));
            }

            return dto.Price.Value;
        }

        private static void ValidateBarcode(string? barcode)
        {
            if (!string.IsNullOrWhiteSpace(barcode) && !barcode.Trim().All(char.IsDigit))
                throw new ArgumentException("Barcode faqat raqamdan iborat bo'lishi kerak", nameof(barcode));
        }
    }
}
