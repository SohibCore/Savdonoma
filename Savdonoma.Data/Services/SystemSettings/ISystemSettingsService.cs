namespace Savdonoma.Data.Services.SystemSetting
{
    public interface ISystemSettingsService
    {
        Task<bool> ReadExist(CancellationToken cancellation);

        Task SaveAsync(bool inventoryEnabled, CancellationToken cancellation);

        Task<bool?> GetInventoryEnabledAsync(CancellationToken cancellation);
    }
}
