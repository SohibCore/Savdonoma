using Savdonoma.Data;
using Savdonoma.Core.Logic;
using Savdonoma.Core.Licensing;
using System.Globalization;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Unit = Savdonoma.Core.Enums.Unit;
using StockMovementType = Savdonoma.Core.Enums.StockMovementType;
using Savdonoma.Data.Services.Stock;

namespace Savdonoma.Import
{
    public class ProductImportService
    {
        private static readonly string[] AllowedUnits = { "dona", "kg", "litr", "metr", "quti" };

        private readonly IDbContextFactory<AppDbContext> _context;
        private readonly ProductReader _reader;
        private readonly ILicenseService _license;

        public ProductImportService(
            IDbContextFactory<AppDbContext> dbContext,
            ProductReader reader,
            ILicenseService license)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _license = license ?? throw new ArgumentNullException(nameof(license));
        }

        // 1-bosqich: o'qish va tekshirish. Bazaga YOZMAYDI.
        public async Task<ProductImportResult> ValidateAsync(string filePath)
        {
            await using var db = await _context.CreateDbContextAsync();
            var result = new ProductImportResult();

            var rows = await Task.Run(() => _reader.Read(filePath));

            var existingProducts = await db.Products
                .AsNoTracking()
                .Select(x => x.Name)
                .ToListAsync();

            var seen = new HashSet<string>(existingProducts.Select(NameNormalizer.Normalize));

            // Barcode bazada unique, shuning uchun takrorlarni yozishdan oldin aniqlaymiz
            var seenBarcodes = new HashSet<string>(
                await db.Products
                    .AsNoTracking()
                    .Where(x => x.Barcode != null)
                    .Select(x => x.Barcode!)
                    .ToListAsync(),
                StringComparer.Ordinal);
            seenBarcodes.UnionWith(await db.ProductBarcodes.AsNoTracking().Select(x => x.Barcode).ToListAsync());

            var categoryIds = (await db.Categories
                .AsNoTracking()
                .Select(x => x.Id)
                .ToListAsync()).ToHashSet();

            foreach (var row in rows)
            {
                // 1. Nomi
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    result.Errors.Add($"{row.RowNumber}-qator: nomi bo'sh");
                    continue;
                }

                // 2. Narx
                if (!TryParsePrice(row.Price, out decimal price))
                {
                    result.Errors.Add($"{row.RowNumber}-qator: narx noto'g'ri (\"{row.Price}\")");
                    continue;
                }

                // Narx butun son sifatida saqlanadi, kasr qismi jim yo'qolmasin
                if (price != Math.Floor(price))
                {
                    result.Errors.Add($"{row.RowNumber}-qator: narx butun son bo'lishi kerak (\"{row.Price}\")");
                    continue;
                }

                // 3. Birlik
                var unit = row.Unit?.Trim().ToLower() ?? "dona";
                if (!AllowedUnits.Contains(unit))
                {
                    result.Errors.Add($"{row.RowNumber}-qator: noma'lum birlik (\"{row.Unit}\")");
                    continue;
                }

                // 4. Miqdor (bo'sh qoldirilsa 0)
                decimal quantity = 0;
                if (!string.IsNullOrWhiteSpace(row.Quantity))
                {
                    if (!TryParseDecimal(row.Quantity, out quantity) || quantity < 0)
                    {
                        result.Errors.Add($"{row.RowNumber}-qator: miqdor noto'g'ri (\"{row.Quantity}\")");
                        continue;
                    }
                }

                // 5. Kategoriya (takrorni tekshirishdan OLDIN bo'lishi shart)
                // Kategoriya katagi to'ldirilgan, lekin son emas
                if (row.CategoryIdText is not null && row.CategoryId is null)
                {
                    result.Errors.Add($"{row.RowNumber}-qator: kategoriya ID son bo'lishi kerak (\"{row.CategoryIdText}\")");
                    continue;
                }
                if (row.CategoryId is not null && !categoryIds.Contains(row.CategoryId.Value))
                {
                    result.Errors.Add($"{row.RowNumber}-qator: kategoriya ID={row.CategoryId} topilmadi");
                    continue;
                }

                // 6. Takror (bazada ham, fayl ichida ham)
                var key = NameNormalizer.Normalize(row.Name);
                if (seen.Contains(key))
                {
                    result.Skipped.Add($"{row.RowNumber}-qator: \"{row.Name}\" allaqachon bor");
                    continue;
                }

                // 7. Barcode takrori (bazada ham, fayl ichida ham)
                var barcode = string.IsNullOrWhiteSpace(row.Barcode) ? null : row.Barcode.Trim();
                if (barcode is not null && seenBarcodes.Contains(barcode))
                {
                    result.Errors.Add($"{row.RowNumber}-qator: barcode \"{barcode}\" boshqa mahsulotda bor");
                    continue;
                }

                seen.Add(key);
                if (barcode is not null)
                    seenBarcodes.Add(barcode);

                result.ValidRows.Add(new ValidProductRow
                {
                    Name = row.Name,
                    Price = price,
                    Unit = unit,
                    CategoryId = row.CategoryId,
                    Barcode = barcode,
                    Quantity = quantity
                });
            }

            return result;
        }

        // 2-bosqich: foydalanuvchi tasdiqlagandan keyin FAQAT to'g'ri qatorlarni yozadi.
        public async Task CommitAsync(ProductImportResult validated)
        {
            if (validated.ValidRows.Count == 0)
                return;

            _license.EnsureCanWrite();

            await StockLedger.Lock.WaitAsync();
            try
            {
                await using var db = await _context.CreateDbContextAsync();
                var now = DateTime.UtcNow;

                var toAdd = validated.ValidRows.Select(row => new
                {
                    Row = row,
                    Product = new Product
                    {
                        Name = row.Name,
                        NameSearch = NameNormalizer.Normalize(row.Name),
                        Price = Convert.ToInt64(row.Price),
                        Unit = Enum.TryParse<Unit>(row.Unit, true, out var parsedUnit) ? parsedUnit : Unit.Dona,
                        CategoryId = row.CategoryId,
                        Barcode = row.Barcode,
                        Quantity = 0,
                        IsActive = true,
                        CreatedAt = now,
                    }
                }).ToList();

                db.Products.AddRange(toAdd.Select(x => x.Product));

                // Fayldagi qoldiq "Boshlang'ich qoldiq" harakati sifatida jurnalga yoziladi
                foreach (var item in toAdd.Where(x => x.Row.Quantity != 0))
                    StockLedger.Apply(db, item.Product, item.Row.Quantity, StockMovementType.OpeningBalance, now, userId: null,
                        note: "Excel import");

                await db.SaveChangesAsync();
                validated.Added = toAdd.Count;
            }
            finally
            {
                StockLedger.Lock.Release();
            }
        }

        public static bool TryParsePrice(string? text, out decimal price)
            => TryParseDecimal(text, out price) && price > 0;

        private static bool TryParseDecimal(string? text, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var clean = text
                .Replace(" ", "")
                .Replace("\u00A0", "")     // buzilmaydigan bo'shliq
                .Replace(",", ".");

            return decimal.TryParse(clean, NumberStyles.Number,
                CultureInfo.InvariantCulture, out value);
        }
    }
}