using Savdonoma.Data;
using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(
                new Category { Id = 1, Name = "Ichimliklar", IsActive = true },
                new Category { Id = 2, Name = "Oziq-ovqat", IsActive = true },
                new Category { Id = 3, Name = "Shirinliklar", IsActive = true },
                new Category { Id = 4, Name = "Sut mahsulotlari", IsActive = true },
                new Category { Id = 5, Name = "Maishiy kimyo", IsActive = true },
                new Category { Id = 6, Name = "Gigiyena", IsActive = true }
            );

            await db.SaveChangesAsync();
        }
    }
}