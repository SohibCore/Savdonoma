using System.Windows;

namespace Savdonoma.BarcodeService
{
    public interface IBarcodeScannerService
    {
        event Action<string>? BarcodeScanned;
        void Attach(UIElement root);     // oynaga ulash
        void Simulate(string code);      // skanersiz sinash uchun
    }
}
