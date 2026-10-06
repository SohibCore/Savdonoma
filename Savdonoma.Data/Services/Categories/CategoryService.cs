using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.Categories
{
    public class CategoryService : ICategoryService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public CategoryService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<CategroyDto>> GetListAsync(CancellationToken cancellation)
        {
            using var db = _factory.CreateDbContext();

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
            using var db = _factory.CreateDbContext();

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
            CreateValidateDto(dto);
            using var db = _factory.CreateDbContext();

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
            using var db = _factory.CreateDbContext();

            var category = db.Categories.SingleOrDefault(x => x.Id == dto.Id && x.IsActive) ?? throw new Exception("Categoriya topilmadi.");

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
            using var db = _factory.CreateDbContext();

            var category = await db.Categories.SingleOrDefaultAsync(x => x.Id == id, cancellation) ?? throw new Exception("Categoriya topilmadi.");

            category.IsActive = false;
            category.UpdatedAt = DateTime.UtcNow;
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
