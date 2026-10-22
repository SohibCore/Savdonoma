using System.Windows;
using Savdonoma.Services;
using Savdonoma.BarcodeService;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Data.Services.SystemSetting;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma.ViewModels
{
    public partial class MainViewModel : ObservableObject // UI ga qandaydir o'zgarishlar bo'lsa, xabar beradi
    {
        private readonly IServiceProvider _sp;
        private readonly ILicenseService _license;
        private readonly CurrentUserService _currentUser;
        private readonly ISystemSettingsService _settings;
        private readonly IBarcodeScannerService _barcodeScanner;
        private int _saleSessionNumber;

        public ObservableCollection<SaleViewModel> SaleSessions { get; } = new();
        public bool CanWrite => _license.Current.IsActive;

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

        [ObservableProperty]
        private bool isEmployeesActive;

        [ObservableProperty]
        private bool isMainMenuExpanded = true;

        public bool IsOwner => _currentUser.IsOwner;

        // Litsenziya banneri va yon menyudagi qisqa holat
        [ObservableProperty]
        private bool showBanner;

        [ObservableProperty]
        private string bannerText = "";

        [ObservableProperty]
        private bool bannerIsError;

        [ObservableProperty]
        private string licenseSummary = "";

        [ObservableProperty]
        private string inventoryModeText = "Rejim yuklanmoqda...";

        [ObservableProperty]
        private string inventoryModeTooltip = "";

        public string TodayText => DateTime.Now.ToString("dd.MM.yyyy • dddd");

        public MainViewModel(
            IServiceProvider sp,
            ILicenseService license,
            CurrentUserService currentUser,
            ISystemSettingsService settings,
            IBarcodeScannerService barcodeScanner)
        {
            _sp = sp;
            _license = license;
            _currentUser = currentUser;
            _settings = settings;
            _barcodeScanner = barcodeScanner;
            _barcodeScanner.BarcodeScanned += OnBarcodeScanned;

            // StatusChanged fon oqimidan keladi, shuning uchun Dispatcher kerak
            _license.StatusChanged += (_, _) =>
                Application.Current.Dispatcher.BeginInvoke(new Action(UpdateLicenseInfo));

            UpdateLicenseInfo();
            _ = LoadInventoryModeAsync();
            AddSaleSession();
        }

        private void OnBarcodeScanned(string code)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                _ = ProcessBarcodeAsync(code);
                return;
            }

            Application.Current.Dispatcher.BeginInvoke(
                new Action(() => _ = ProcessBarcodeAsync(code)));
        }

        private async Task ProcessBarcodeAsync(string code)
        {
            if (IsSaleActive && CurrentSaleSession is { } session)
                await session.ProcessScannedBarcodeAsync(code);
        }

        private async Task LoadInventoryModeAsync()
        {
            try
            {
                var inventoryEnabled = await _settings.GetInventoryEnabledAsync(CancellationToken.None);
                if (inventoryEnabled is null)
                    throw new InvalidOperationException("Do'kon rejimi sozlanmagan.");

                InventoryModeText = inventoryEnabled.Value
                    ? "Qoldiq bilan"
                    : "Qoldiqsiz";
                InventoryModeTooltip = "Do'kon rejimi";
            }
            catch (Exception ex)
            {
                InventoryModeText = "Rejim xatosi";
                InventoryModeTooltip = ex.Message;
            }
        }

        private void UpdateLicenseInfo()
        {
            var s = _license.Current;

            ShowBanner = s.Status is LicenseStatus.ExpiringSoon
                or LicenseStatus.Expired
                or LicenseStatus.Invalid
                or LicenseStatus.WrongMachine;
            BannerIsError = s.Status is LicenseStatus.Expired
                or LicenseStatus.Invalid
                or LicenseStatus.WrongMachine;
            BannerText = BannerIsError
                ? s.Message + " Faqat ko'rish rejimi: sotuv va o'zgartirishlar o'chirilgan."
                : s.Message;
            OnPropertyChanged(nameof(CanWrite));
            AddSaleSessionCommand.NotifyCanExecuteChanged();

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

        private void SetActiveSection(bool sale = false, bool products = false, bool reports = false, bool license = false, bool employees = false)
        {
            IsSaleActive = sale;
            IsProductsActive = products;
            IsReportsActive = reports;
            IsLicenseActive = license;
            IsEmployeesActive = employees;
        }

        [RelayCommand]
        private void ToggleMainMenu()
        {
            IsMainMenuExpanded = !IsMainMenuExpanded;
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

        [RelayCommand(CanExecute = nameof(CanWrite))]
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
            if (!_currentUser.IsOwner)
                return;

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

        [RelayCommand]
        private async Task ShowEmployeesAsync()
        {
            if (!_currentUser.IsOwner)
                return;

            var viewModel = _sp.GetRequiredService<EmployeesViewModel>();
            CurrentPage = viewModel;
            PageTitle = "Xodimlar";
            PageSubtitle = "Xodimlar va ularning tizimdagi rollarini boshqaring";
            SetActiveSection(employees: true);
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }
}