using Savdonoma.Core.Entity;
using Savdonoma.Data.Services.SystemSettings;

namespace Savdonoma.Data.Services.SystemSetting
{
    public interface ISystemSettingsService
    {
        Task<bool> ReadExist(CancellationToken cancellation);

        Task SaveAsync(bool inventoryEnabled, CancellationToken cancellation);

        Task<bool?> GetInventoryEnabledAsync(CancellationToken cancellation);

        Task<UpdateSystemSettingsDto> GetInventorySettingsAsync(CancellationToken cancellation);

        Task<Savdonoma.Core.Entity.SystemSettings> GetSystemSettings(int settingsId, CancellationToken cancellation);

        Task<bool> Update(UpdateSystemSettingsDto dto, CancellationToken cancellation);
    }
}
