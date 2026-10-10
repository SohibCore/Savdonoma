using System.IO;
using System.Windows;
using Microsoft.Win32;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class LicenseViewModel : ObservableObject
    {
        private readonly ILicenseService _license;

        //[ObservableProperty] - bu CommunityToolkit.Mvvm kutubxonasidagi atribut bo‘lib, oddiy private field asosida avtomatik ravishda public property yaratadi va uning qiymati o‘zgarganda UI'ga xabar beradi.

        [ObservableProperty] private string shopName = "";
        [ObservableProperty] private string statusText = "";  // Litsenziya holatini ko‘rsatadi
        [ObservableProperty] private string expiryText = "";  // Litsenziyaning amal qilish muddatini ko‘rsatadi
        [ObservableProperty] private string daysLeftText = ""; //Litsenziya tugashigacha qolgan kunlarni ko‘rsatadi
        [ObservableProperty] private string licenseKey = ""; //Kiritilgan litsenziya kalitini saqlaydi
        [ObservableProperty] private string resultMessage = ""; //Natija yoki xatolik xabarini ko‘rsatadi
        [ObservableProperty] private bool resultIsError; //Natija xatolik ekanligini bildiruvchi bool qiymat

        // Faollashtirish oynasi shu event orqali yopiladi
        public event Action? Activated;

        public LicenseViewModel(ILicenseService license)
        {
            _license = license;
            _license.StatusChanged += (_, _) =>
                Application.Current.Dispatcher.BeginInvoke(new Action(Update));
            Update();
        }

        private void Update()
        {
            var s = _license.Current;
            ShopName = s.ShopName ?? "-";
            StatusText = s.Message;

            if (s.IsPerpetual)
                ExpiryText = "Doimiy (muddatsiz)";
            else
                ExpiryText = s.ExpiresOn?.ToString("dd.MM.yyyy") ?? "-";

            DaysLeftText = s.IsPerpetual || s.ExpiresOn == null ? "-" : $"{s.DaysLeft} kun";
        }

        [RelayCommand] // - avtomatik ravishda ActivateCommand nomli command yaratadi. Uni WPF XAML'da tugmaga bog‘lash mumkin:
        private void Activate() // litsenziya kalitini tekshirish va litsenziyani faollashtirish
        {
            if (string.IsNullOrWhiteSpace(LicenseKey))
            {
                SetResult("Litsenziya kalitini kiriting yoki fayl tanlang.", true);
                return;
            }

            var state = _license.Activate(LicenseKey);
            Update();

            if (state.IsActive)
            {
                LicenseKey = "";
                SetResult("Litsenziya faollashtirildi.", false);
                Activated?.Invoke(); // Boshqa kod qismlariga faollashtirish muvaffaqiyatli bo'lganini xabar berish
            }
            else
            {
                SetResult(state.Message, true);
            }
        }

        [RelayCommand]
        private void LoadFromFile()
        {
            var dialog = new OpenFileDialog // Windows fayl tanlash oynasi.
            { 
                Filter = "Savdonoma litsenziyasi (*.lic)|*.lic",
                DefaultExt = ".lic",
                CheckFileExists = true
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                LicenseKey = File.ReadAllText(dialog.FileName);
            }
            catch (Exception ex)
            {
                SetResult("Faylni o'qib bo'lmadi: " + ex.Message, true);
                return;
            }

            Activate();
        }

        private void SetResult(string text, bool isError)
        {
            ResultMessage = text;
            ResultIsError = isError;
        }
    }
}