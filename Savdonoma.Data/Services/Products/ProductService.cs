using Savdonoma.Core.Logic;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ProductService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
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
                    CategoryId = x.CategoryId,
                    Barcode = x.Barcode,
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
            ValidateCreating(dto);
            using var db = _factory.CreateDbContext();

            var barcode = CleanBarcode(dto.Barcode);
            var now = DateTime.UtcNow;

            var product = new Product()
            {
                Name = dto.Name.Trim(),
                NameSearch = NameNormalizer.Normalize(dto.Name),
                Unit = dto.Unit,
                Price = dto.Price,
                CategoryId = null,//dto.CategoryId,
                Barcode = barcode,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await db.Products.AddAsync(product, cancellation);
            await db.SaveChangesAsync(cancellation);

            return ReturnDto(product);
        }

        public async Task DeactivateAsync(int Id, CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == Id, cancellation) ?? throw new Exception("Mahsulot topilmadi");

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
        }

        public async Task<ProductDto> UpdateAsync(UpdateProductDto dto, CancellationToken cancellation)
        {
            ValidateUpdating(dto);
            using var db = _factory.CreateDbContext();
            var product = await db.Products.FirstOrDefaultAsync(x => x.Id == dto.Id, cancellation) ?? throw new Exception("Mahsulot topilmadi");

            var barcode = CleanBarcode(dto.Barcode);

            if (dto.Name != null)
            {
                product.Name = dto.Name.Trim();
                product.NameSearch = NameNormalizer.Normalize(dto.Name);
            }
            if (dto.Barcode != null)
                product.Barcode = barcode;
            if (dto.Unit.HasValue)
                product.Unit = dto.Unit.Value;
            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;
            if (dto.IsActive.HasValue)
                product.IsActive = dto.IsActive.Value;
            if (dto.CategoryId.HasValue)
                product.CategoryId = dto.CategoryId.Value;

            product.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
            return ReturnDto(product);
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
                Barcode = p.Barcode
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
        private static void ValidateUpdating(UpdateProductDto dto)
        {
            if (dto.Name != null && string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Mahsulot nomi bo'sh bo'lmasligi kerak", nameof(dto.Name));
            }
            if (dto.Price.HasValue && dto.Price <= 0)
            {
                throw new ArgumentException("Mahsulot narxi 0 dan katta bo'lishi kerak", nameof(dto.Price));
            }
            /*if (dto.CategoryId.HasValue && dto.CategoryId <= 0)
            {
                throw new ArgumentException("Mahsulot uchun kategoriya tanlash shart", nameof(dto.CategoryId));
            }*/
            ValidateBarcode(dto.Barcode);
        }
        private static void ValidateBarcode(string? barcode)
        {
            if (!string.IsNullOrWhiteSpace(barcode) && !barcode.Trim().All(char.IsDigit))
                throw new ArgumentException("Barcode faqat raqamdan iborat bo'lishi kerak", nameof(barcode));
        }
    }
}
