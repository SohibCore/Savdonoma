using Savdonoma.Core.Entity;
using Savdonoma.Core.Licensing;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILicenseService _license;
        public CategoryService(IDbContextFactory<AppDbContext> factory, ILicenseService license)
        {
            _factory = factory;
            _license = license;
        }

        public async Task<List<CategroyDto>> GetListAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var categories = await db.Categories
                .Where(x => x.IsActive)
                .Select(x => new CategroyDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                }).ToListAsync(cancellation);

            return categories;
        }

        public async Task<CategroyDto> GetAsync(int Id, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var product = await db.Categories
                .Where(x => x.Id == Id && x.IsActive)
                .Select(x => new CategroyDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    IsActive = x.IsActive,
                }).FirstOrDefaultAsync(cancellation);

            if (product is null)
                throw new Exception("Categoriya topilmadi.");

            return product;
        }

        public async Task<CategroyDto> CreateAsync(CreateCategoryDto dto, CancellationToken cancellation)
        {
            _license.EnsureCanWrite();
            CreateValidateDto(dto);
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var category = new Category
            {
                Name = dto.Name,
                IsActive = true,

                CreatedAt = DateTime.UtcNow,
            };

            await db.Categories.AddAsync(category, cancellation);
            await db.SaveChangesAsync(cancellation);

            return new CategroyDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
            };
        }

        public async Task<CategroyDto> UpdateAsync(UpdateCategoryDto dto, CancellationToken cancellation)
        {
            _license.EnsureCanWrite();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var category = db.Categories
                .SingleOrDefault(x => x.Id == dto.Id && x.IsActive) ?? throw new Exception("Categoriya topilmadi.");

            if (!string.IsNullOrWhiteSpace(dto.Name))
                category.Name = dto.Name;

            category.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);

            return new CategroyDto
            {
                Id = category.Id,
                Name = category.Name,
                IsActive = category.IsActive,
            };
        }

        public async Task<bool> Delete(int id, CancellationToken cancellation)
        {
            _license.EnsureCanWrite();
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, cancellation) ?? throw new Exception("Categoriya topilmadi.");

            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
            return true;
        }

        //Yordamchi funksiyalar
        private static void CreateValidateDto(CreateCategoryDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new Exception("Nomni kiritish shart.");
            }
        }
    }
}
