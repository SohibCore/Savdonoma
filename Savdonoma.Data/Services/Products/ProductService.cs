
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens.Experimental;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Logic;

namespace Savdonoma.Data.Services.Products
{
    public class ProductService : IProductService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ProductService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }
        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            Validate(dto);
            using var db = _factory.CreateDbContext();

            var now = DateTime.UtcNow;

            var product = new Product()
            {
                Name = dto.Name.Trim(),
                NameSearch = NameNormalizer.Normalize(dto.Name),
                Unit = dto.Unit,
                Price = dto.Price,
                CategoryId = dto.CategoryId,
                Barcode = dto.Barcode,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now
            };

            await db.Products.AddAsync(product);
            await db.SaveChangesAsync();

            return ReturnDto(product);
        }

        public Task DeleteAsync(int Id)
        {
            throw new NotImplementedException();
        }

        public Task<ProductDto> GetAsync(int Id)
        {
            throw new NotImplementedException();
        }

        public Task<List<ProductDto>> GetListAsync()
        {
            throw new NotImplementedException();
        }

        public Task<List<ProductDto>> SearchAsync(string? text, int take = 50)
        {
            throw new NotImplementedException();
        }

        public Task<ProductDto> UpdateAsync(int Id, UpdateProductDto dto)
        {
            throw new NotImplementedException();
        }
        public static void Validate(CreateProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException("Mahsulot nomi bo'sh bo'lmasligi kerak", nameof(dto.Name));
            }
            if (dto.Price <= 0)
            {
                throw new ArgumentException("Mahsulot narxi 0 dan katta bo'lishi kerak", nameof(dto.Price));
            }
            if (dto.CategoryId <= 0)
            {
                throw new ArgumentException("Mahsulot uchun kategoriya tanlash shart", nameof(dto.CategoryId));
            }
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
    }
}
