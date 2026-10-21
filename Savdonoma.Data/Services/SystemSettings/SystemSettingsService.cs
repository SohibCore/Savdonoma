using Savdonoma.Core.Entity;
using Microsoft.EntityFrameworkCore;

namespace Savdonoma.Data.Services.SystemSetting
{
    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        public SystemSettingsService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<bool?> GetInventoryEnabledAsync(CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);

            var setting = await db.SysSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellation);

            return setting?.InventoryEnabled;
        }

        public async Task<SystemSettings> GetSystemSettings(int settingsId, CancellationToken cancellation)
        {
            await using var db = await _factory.CreateDbContextAsync(cancellation);
            var printerInfo = await db.SysSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == settingsId, cancellation) ?? throw new Exception("Printer topilmadi.");

            var printer = new SystemSettings
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
                var setting = new SystemSettings
                {
                    InventoryEnabled = inventoryEnabled,
                    CreatedAt = DateTime.UtcNow,
                };

                await db.SysSettings.AddAsync(setting);
                await db.SaveChangesAsync(cancellation);
            }
        }
    }
}
