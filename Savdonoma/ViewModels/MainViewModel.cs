using System.Windows;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma.ViewModels
{
    public partial class MainViewModel : ObservableObject // UI ga qandaydir o'zgarishlar bo'lsa, xabar beradi
    {
        private readonly IServiceProvider _sp;
        private readonly ILicenseService _license;
        private int _saleSessionNumber;

        public ObservableCollection<SaleViewModel> SaleSessions { get; } = new();

        [ObservableProperty] // CurrentPage o'zgarganda UI ham o'zgarishi kerak
        private object? currentPage;

        [ObservableProperty]
        private SaleViewModel? currentSaleSession;

        [ObservableProperty]
        private string pageTitle = "Sotuv terminali";

        [ObservableProperty]
        private string pageSubtitle = "Mahsulot tanlang va savdoni yakunlang";

        [ObservableProperty]
        private bool isSaleActive = false;

        [ObservableProperty]
        private bool isProductsActive = false;

        [ObservableProperty]
        private bool isReportsActive = true;

        [ObservableProperty]
        private bool isLicenseActive = false;

        // Litsenziya banneri va yon menyudagi qisqa holat
        [ObservableProperty]
        private bool showBanner;

        [ObservableProperty]
        private string bannerText = "";

        [ObservableProperty]
        private bool bannerIsError;

        [ObservableProperty]
        private string licenseSummary = "";

        public string TodayText => DateTime.Now.ToString("dd.MM.yyyy • dddd");

        public MainViewModel(IServiceProvider sp, ILicenseService license)
        {
            _sp = sp;
            _license = license;

            // StatusChanged fon oqimidan keladi, shuning uchun Dispatcher kerak
            _license.StatusChanged += (_, _) =>
                Application.Current.Dispatcher.BeginInvoke(new Action(UpdateLicenseInfo));

            UpdateLicenseInfo();
            AddSaleSession();
        }

        private void UpdateLicenseInfo()
        {
            var s = _license.Current;

            ShowBanner = s.Status is LicenseStatus.ExpiringSoon or LicenseStatus.Expired;
            BannerIsError = s.Status == LicenseStatus.Expired;
            BannerText = s.Status == LicenseStatus.Expired
                ? s.Message + " Faqat ko'rish rejimi: sotuv va o'zgartirishlar o'chirilgan."
                : s.Message;

            LicenseSummary = s.Status switch
            {
                LicenseStatus.Valid when s.IsPerpetual => "Doimiy",
                LicenseStatus.Valid => $"{s.ExpiresOn:dd.MM.yyyy} gacha",
                LicenseStatus.ExpiringSoon => s.DaysLeft == 0 ? "Bugun tugaydi" : $"{s.DaysLeft} kun qoldi",
                LicenseStatus.Expired => "Muddati tugagan",
                LicenseStatus.NoLicense => "Faollashtirilmagan",
                _ => "Noto'g'ri litsenziya"
            };
        }

        private void SetActiveSection(bool sale = false, bool products = false, bool reports = false, bool license = false)
        {
            IsSaleActive = sale;
            IsProductsActive = products;
            IsReportsActive = reports;
            IsLicenseActive = license;
        }

        partial void OnCurrentSaleSessionChanged(SaleViewModel? value)
        {
            if (IsSaleActive && value != null)
                CurrentPage = value;
        }

        [RelayCommand]
        private void ShowSale()
        {
            SetActiveSection(sale: true);
            CurrentSaleSession ??= SaleSessions.FirstOrDefault();
            if (CurrentSaleSession == null)
                AddSaleSession();
            else
                CurrentPage = CurrentSaleSession;

            PageTitle = "Sotuv terminali";
            PageSubtitle = "Mahsulot tanlang va savdoni yakunlang";
        }

        [RelayCommand]
        private void AddSaleSession()
        {
            var session = _sp.GetRequiredService<SaleViewModel>();
            session.SessionTitle = $"Sotuv {++_saleSessionNumber}";
            SaleSessions.Add(session);
            SetActiveSection(sale: true);
            CurrentSaleSession = session;
            CurrentPage = session;
            PageTitle = "Sotuv terminali";
            PageSubtitle = "Mahsulot tanlang va savdoni yakunlang";
        }

        [RelayCommand]
        private void CloseSaleSession(SaleViewModel? session)
        {
            if (session == null || SaleSessions.Count <= 1)
                return;

            var index = SaleSessions.IndexOf(session);
            SaleSessions.Remove(session);

            if (CurrentSaleSession == session)
                CurrentSaleSession = SaleSessions[Math.Min(index, SaleSessions.Count - 1)];

            session.Dispose();   // litsenziya eventidan obunani olib tashlaydi (pastga qarang)
        }

        [RelayCommand]
        private void ShowProducts()
        {
            CurrentPage = _sp.GetRequiredService<ProductsViewModel>();
            PageTitle = "Mahsulotlar";
            PageSubtitle = "Katalog, narx va qoldiqni boshqaring";
            SetActiveSection(products: true);
        }

        [RelayCommand]
        private void ShowReports()
        {
            CurrentPage = _sp.GetRequiredService<ReportsViewModel>();
            PageTitle = "Hisobotlar";
            PageSubtitle = "Kunlik, haftalik va oylik tahlil";
            SetActiveSection(reports: true);
            _ = ((ReportsViewModel)CurrentPage).LoadAsync();
        }

        [RelayCommand]
        private void ShowLicense()
        {
            CurrentPage = _sp.GetRequiredService<LicenseViewModel>();
            PageTitle = "Litsenziya";
            PageSubtitle = "Dastur litsenziyasi va faollashtirish";
            SetActiveSection(license: true);
        }
    }
}