using Microsoft.EntityFrameworkCore;
using Savdonoma.Core.Entity;
using Savdonoma.Data.Services.SystemSettings;
using Savdonoma.Services;

namespace Savdonoma.Data.Services.SystemSetting
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly CurrentUserService _currentUser;

        public SystemSettingsService(IDbContextFactory<AppDbContext> factory)
            : this(factory, new CurrentUserService())
        {
        }

        public SystemSettingsService(IDbContextFactory<AppDbContext> factory, CurrentUserService currentUser)
        {
            _factory = factory;
            _currentUser = currentUser;
        }

        public async Task<bool?> GetInventoryEnabledAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var setting = await db.SysSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellation);

            return setting?.InventoryEnabled;
        }

        public async Task<UpdateSystemSettingsDto> GetInventorySettingsAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var setting = await db.SysSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellation)
                ?? throw new InvalidOperationException("Do‘kon rejimi hali sozlanmagan.");

            return new UpdateSystemSettingsDto
            {
                Id = setting.Id,
                InventoryEnabled = setting.InventoryEnabled,
            };
        }

        public async Task<Savdonoma.Core.Entity.SystemSettings> GetSystemSettings(int settingsId, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var printerInfo = await db.SysSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == settingsId, cancellation) ?? throw new Exception("Printer topilmadi.");

            var printer = new Savdonoma.Core.Entity.SystemSettings
            {
                ShopName = printerInfo.ShopName,
                Address = printerInfo.Address,
                PrinterName = printerInfo.PrinterName,
                CheckLength = printerInfo.CheckLength,
            };

            return printer;
        }

        public async Task<bool> ReadExist(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            return await db.SysSettings.AnyAsync(cancellation);
        }

        public async Task SaveAsync(bool inventoryEnabled, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var exist = await db.SysSettings.AnyAsync(cancellation).ConfigureAwait(false);

            if (exist == false)
            {
                var setting = new Savdonoma.Core.Entity.SystemSettings
                {
                    InventoryEnabled = inventoryEnabled,
                    CreatedAt = DateTime.UtcNow,
                };

                await db.SysSettings.AddAsync(setting);
                await db.SaveChangesAsync(cancellation);
            }
        }

        public async Task<bool> Update(UpdateSystemSettingsDto dto, CancellationToken cancellation)
        {
            EnsureOwner();
            if (!dto.InventoryEnabled.HasValue)
                throw new InvalidOperationException("Yangilanadigan sozlama qiymati kiritilmagan.");

            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var setting = await db.SysSettings
                .SingleOrDefaultAsync(x => x.Id == dto.Id, cancellation)
                ?? throw new InvalidOperationException("Sozlama topilmadi.");

            setting.InventoryEnabled = dto.InventoryEnabled.Value;
            setting.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellation);
            return true;
        }

        private void EnsureOwner()
        {
            if (!_currentUser.IsOwner)
                throw new UnauthorizedAccessException("Sozlamalarni faqat do‘kon egasi o‘zgartira oladi.");
        }
    }
}
