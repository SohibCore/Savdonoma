namespace Savdonoma.Core.Licensing
{
    public interface ILicenseService
    {
        LicenseState Current { get; }
        event EventHandler<LicenseState>? StatusChanged;

        LicenseState Refresh();                    // sanani qayta tekshiradi
        LicenseState Activate(string licenseKey);  // kalit to'g'ri bo'lsa saqlaydi
        void EnsureCanWrite();                     // faol bo'lmasa xato tashlaydi
    }
}