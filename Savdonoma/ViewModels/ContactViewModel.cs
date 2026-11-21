using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Savdonoma.ViewModels
{
    // "Bog'lanish" sahifasi: qo'llab-quvvatlash kontaktlari
    public partial class ContactViewModel : ObservableObject
    {
        public string PhoneText => "+998 77 157-77-30";
        public string PhoneDial => "+998771577730";
        public string TelegramUser => "bekmurodv_s";
        public string TelegramText => "@" + TelegramUser;
        public string WorkDaysText => "Dushanbadan Shanbagacha";
        public string WorkHoursText => "09:00 – 18:00";

        [ObservableProperty] private string statusMessage = "";

        [RelayCommand]
        private void CopyPhone()
        {
            try
            {
                Clipboard.SetText(PhoneDial);
                StatusMessage = "Telefon raqami nusxalandi";
            }
            catch
            {
                StatusMessage = "";
            }
        }

        [RelayCommand]
        private void OpenTelegram()
        {
            try
            {
                Process.Start(new ProcessStartInfo($"https://t.me/{TelegramUser}") { UseShellExecute = true });
            }
            catch
            {
                Clipboard.SetText(TelegramText);
                StatusMessage = "Telegram ochilmadi, username nusxalandi";
            }
        }
    }
}
