using System.Windows;
using Savdonoma.Services;
using Savdonoma.BarcodeService;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Data.Services.SystemSettings;
using Savdonoma.Views;
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
        private bool isOrganizationActive;

        [ObservableProperty]
        private bool isWarehouseActive;

        [ObservableProperty]
        private bool isMainMenuExpanded = true;

        public bool IsOwner => _currentUser.IsOwner;
        public bool CanManageStock => _currentUser.CanManageStock;

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
                InventoryModeTooltip = "Do‘kon rejimini o‘zgartirish uchun bosing";
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

        private void SetActiveSection(bool sale = false, bool products = false, bool reports = false, bool license = false, bool employees = false, bool warehouse = false, bool organization = false)
        {
            IsSaleActive = sale;
            IsProductsActive = products;
            IsReportsActive = reports;
            IsLicenseActive = license;
            IsEmployeesActive = employees;
            IsWarehouseActive = warehouse;
            IsOrganizationActive = organization;
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

            var products = _sp.GetRequiredService<ProductsViewModel>();
            CurrentPage = products;
            // Kirim/sotuvdan keyin qoldiq, narx va tannarx yangi bo'lishi uchun har safar yangilanadi
            if (products.RefreshProductsCommand.CanExecute(null))
                products.RefreshProductsCommand.Execute(null);
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
        private async Task ShowWarehouseAsync()
        {
            if (!_currentUser.CanManageStock)
                return;

            var viewModel = _sp.GetRequiredService<Warehouse.WarehouseViewModel>();
            CurrentPage = viewModel;
            PageTitle = "Ombor";
            PageSubtitle = "Kirim, hisobdan chiqarish, inventarizatsiya va yetkazib beruvchilar";
            SetActiveSection(warehouse: true);
            await viewModel.LoadAsync();
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

        [RelayCommand]
        private async Task ShowOrganizationAsync()
        {
            if (!_currentUser.IsOwner)
                return;

            var viewModel = _sp.GetRequiredService<OrganizationViewModel>();
            CurrentPage = viewModel;
            PageTitle = "Do‘kon haqida";
            PageSubtitle = "Do‘kon va egasi ma’lumotlari";
            SetActiveSection(organization: true);
            await viewModel.LoadAsync();
        }

        [RelayCommand]
        private async Task ChangeInventoryModeAsync()
        {
            if (!_currentUser.IsOwner)
                return;

            try
            {
                var settings = await _settings.GetInventorySettingsAsync(CancellationToken.None);
                var window = new InventorySetupWindow(settings.InventoryEnabled == true)
                {
                    Owner = Application.Current.MainWindow
                };

                if (window.ShowDialog() != true)
                    return;

                await _settings.Update(new UpdateSystemSettingsDto
                {
                    Id = settings.Id,
                    InventoryEnabled = window.InventoryEnabled,
                }, CancellationToken.None);

                await LoadInventoryModeAsync();

                foreach (var session in SaleSessions)
                    await session.ReloadInventoryModeAsync();

                if (CurrentPage is ProductsViewModel products)
                {
                    await products.ReloadInventoryModeAsync();
                    await products.RefreshProductsCommand.ExecuteAsync(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Do‘kon rejimini saqlashda xatolik",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}