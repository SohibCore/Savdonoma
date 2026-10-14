using Savdonoma.Services;
using Savdonoma.Core.Logic;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;
using Savdonoma.Data.Services.SystemSetting;
using System.Security.Cryptography.X509Certificates;

namespace Savdonoma.Data.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ISystemSettingsService _settings;
        private readonly CurrentUserService _currentUser;

        public ProductService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser, ISystemSettingsService system)
        {
            _factory = factory;
            _currentUser = currentUser;
            _settings = system;
        }
        public async Task<List<ProductDto>> GetListAsync(CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var products = db.Products
                .Where(x => x.IsActive != false);

            return await products
                .OrderBy(x => x.Name)
                .Select(x => new ProductDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Unit = x.Unit,
                    Price = x.Price,
                    Barcode = x.Barcode,
                    Quantity = x.Quantity,
                    CategoryId = x.CategoryId,
                }).ToListAsync(cancellation);
        }

        public async Task<ProductDto> GetAsync(int Id, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var product = await db.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == Id, cancellation) ?? throw new Exception("Mahsulot topilmadi.");

            return ReturnDto(product);
        }

        public async Task<List<ProductDto>> SearchAsync(string? text, CancellationToken cancellation, int take = 50)
        {
            using var db = _factory.CreateDbContext();

            var product = db.Products
                .AsNoTracking()
                .Where(x => x.IsActive);

            var key = NameNormalizer.Normalize(text);
            var barcode = text?.Trim();

            if (key != "")
                product = product.Where(x => x.NameSearch.Contains(key) || x.Barcode == barcode);

            var items = await product
                .OrderBy(x => x.NameSearch)
                .Take(take).ToListAsync(cancellation);

            return items.Select(ReturnDto).ToList();
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            ValidateCreating(dto);

            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation);

            if (inventoryEnabled is null)
                throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

            using var db = _factory.CreateDbContext();

            var barcode = CleanBarcode(dto.Barcode);
            var now = DateTime.UtcNow;

            if (inventoryEnabled.Value && dto.Quantity is < 0)
                throw new ArgumentException("Mahsulot qoldig'i manfiy bo'lishi mumkin emas.");

            var quantity = inventoryEnabled.Value ? dto.Quantity : 0m;

            var product = new Product()
            {
                Name = dto.Name.Trim(),
                NameSearch = NameNormalizer.Normalize(dto.Name),
                Unit = dto.Unit,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                Barcode = barcode,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                Quantity = quantity,
            };

            await db.Products.AddAsync(product, cancellation);
            await db.SaveChangesAsync(cancellation);

            return ReturnDto(product);
        }

        public async Task DeactivateAsync(int Id, CancellationToken cancellation)
        {
            EnsureOwner();
            using var db = _factory.CreateDbContext();

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == Id, cancellation) ?? throw new Exception("Mahsulot topilmadi");

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
        }

        public async Task<ProductDto> UpdateAsync(UpdateProductDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            var price = ValidateUpdating(dto);
            var inventoryEnabled = await _settings.GetInventoryEnabledAsync(cancellation);
            if (inventoryEnabled is null)
                throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

            if (inventoryEnabled.Value && dto.Quantity < 0)
                throw new ArgumentException("Mahsulot qoldig'i manfiy bo'lishi mumkin.", nameof(dto.Quantity));

            using var db = _factory.CreateDbContext();
            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellation) ?? throw new Exception("Mahsulot topilmadi");

            product.Price = price;
            product.Quantity = inventoryEnabled.Value
                ? dto.Quantity ?? product.Quantity
                : 0m;

            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
            return ReturnDto(product);
        }

        private void EnsureOwner()
        {
            if (!_currentUser.IsOwner)
                throw new UnauthorizedAccessException("Mahsulotlarni faqat do‘kon egasi boshqara oladi.");
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
            /*if (dto.CategoryId <= 0)
            {
                throw new ArgumentException("Mahsulot uchun kategoriya tanlash shart", nameof(dto.CategoryId));
            }*/
            ValidateBarcode(dto.Barcode);
        }
        private static ProductDto ReturnDto(Product p)
        {
            var response = new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                NameSearch = p.NameSearch,
                Unit = p.Unit,
                Price = p.Price,
                IsActive = p.IsActive,
                CategoryId = p.CategoryId,
                Barcode = p.Barcode,
                Quantity = p.Quantity
            };
            return response;
        }
        private static string? CleanBarcode(string? barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return null;
            }
            return barcode.Trim();
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
