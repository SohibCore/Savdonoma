using Savdonoma.Services;
using Savdonoma.Converters;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Data.Services.DashboardService;

namespace Savdonoma.ViewModels
{
    public enum DashboardPeriod
    {
        Today,
        Week,
        Month
    }

    // Top mahsulot qatori: Share - eng yaxshi mahsulotga nisbatan ulush (0..1), progress chiziq uchun
    public class DashboardTopProductRow
    {
        public int Rank { get; init; }
        public string Name { get; init; } = "";
        public string QuantityText { get; init; } = "";
        public long Amount { get; init; }
        public double Share { get; init; }
    }

    public class DashboardEmployeeRow
    {
        public string Name { get; init; } = "";
        public string Initials { get; init; } = "";
        public string RoleText { get; init; } = "";
        public int SalesCount { get; init; }
        public long TotalAmount { get; init; }
        public long AverageCheck { get; init; }
        public long CashTotal { get; init; }
        public long CardTotal { get; init; }
        public int CancelledCount { get; init; }
        public double SharePercent { get; init; }   // umumiy savdodagi ulushi, 0..100
        public bool HasSales => SalesCount > 0;
    }

    public class DashboardLowStockRow
    {
        public string Name { get; init; } = "";
        public string QuantityText { get; init; } = "";
        public string MinQuantityText { get; init; } = "";
        public bool IsOut { get; init; }
    }

    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IDashboardService _dashboard;
        private readonly ISystemSettingsService _settings;
        private readonly CurrentUserService _currentUser;
        private CancellationTokenSource? _cts;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsToday), nameof(IsWeek), nameof(IsMonth), nameof(PeriodText))]
        private DashboardPeriod period = DashboardPeriod.Today;

        public bool IsToday => Period == DashboardPeriod.Today;
        public bool IsWeek => Period == DashboardPeriod.Week;
        public bool IsMonth => Period == DashboardPeriod.Month;

        [ObservableProperty] private string periodText = "";
        [ObservableProperty] private bool isLoading;
        [ObservableProperty] private string errorMessage = "";

        // Savdo ko'rsatkichlari
        [ObservableProperty] private long totalAmount;
        [ObservableProperty] private int salesCount;
        [ObservableProperty] private long averageCheck;
        [ObservableProperty] private int cancelledCount;

        [ObservableProperty] private int cashCount;
        [ObservableProperty] private long cashTotal;
        [ObservableProperty] private int cardCount;
        [ObservableProperty] private long cardTotal;
        [ObservableProperty] private double cashShare;   // 0..100
        [ObservableProperty] private double cardShare;   // 0..100

        // Top mahsulotlar
        [ObservableProperty] private ObservableCollection<DashboardTopProductRow> topProducts = new();
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NoTopProducts))]
        private bool hasTopProducts;
        public bool NoTopProducts => !HasTopProducts;

        // Ogohlantirishlar (faqat qoldiq bilan ishlash rejimida)
        [ObservableProperty] private bool showStockWarnings;
        [ObservableProperty] private int lowStockCount;
        [ObservableProperty] private int outOfStockCount;
        [ObservableProperty] private ObservableCollection<DashboardLowStockRow> lowStockItems = new();
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NoLowStock))]
        private bool hasLowStock;
        public bool NoLowStock => !HasLowStock;

        // Xodimlar kesimida savdo (faqat rahbar va menejer).
        // O'z sana filtri bor: Bugun/Hafta/Oy tanlanganda shu davrga moslashadi, keyin alohida o'zgartirish mumkin.
        [ObservableProperty] private bool showEmployeeSales;
        [ObservableProperty] private ObservableCollection<DashboardEmployeeRow> employeeSales = new();
        [ObservableProperty] private DateTime? employeeFromDate = DateTime.Today;
        [ObservableProperty] private DateTime? employeeToDate = DateTime.Today;
        [ObservableProperty] private bool isEmployeeLoading;
        [ObservableProperty] private string employeeErrorMessage = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NoEmployeeSales))]
        private bool hasEmployeeSales;
        public bool NoEmployeeSales => !HasEmployeeSales;

        public bool IsEmployeeToday => EmployeeFromDate == DateTime.Today && EmployeeToDate == DateTime.Today;
        public bool IsEmployeeYesterday => EmployeeFromDate == DateTime.Today.AddDays(-1) && EmployeeToDate == DateTime.Today.AddDays(-1);

        private CancellationTokenSource? _employeeCts;
        private bool _suppressEmployeeReload;

        public string SalesScopeText => _currentUser.CanViewAllSales
            ? "Barcha xodimlar savdosi"
            : "Faqat sizning savdolaringiz";

        public DashboardViewModel(
            IDashboardService dashboard,
            ISystemSettingsService settings,
            CurrentUserService currentUser)
        {
            _dashboard = dashboard;
            _settings = settings;
            _currentUser = currentUser;
        }

        [RelayCommand]
        private async Task SelectPeriodAsync(DashboardPeriod value)
        {
            Period = value;
            await LoadAsync();
        }

        [RelayCommand]
        private Task RefreshAsync() => LoadAsync();

        partial void OnEmployeeFromDateChanged(DateTime? value) => OnEmployeeRangeChanged();
        partial void OnEmployeeToDateChanged(DateTime? value) => OnEmployeeRangeChanged();

        private void OnEmployeeRangeChanged()
        {
            OnPropertyChanged(nameof(IsEmployeeToday));
            OnPropertyChanged(nameof(IsEmployeeYesterday));
            if (!_suppressEmployeeReload)
                _ = LoadEmployeeSalesAsync();
        }

        [RelayCommand]
        private Task EmployeeTodayAsync() => SetEmployeeRangeAsync(DateTime.Today, DateTime.Today);

        [RelayCommand]
        private Task EmployeeYesterdayAsync()
        {
            var day = DateTime.Today.AddDays(-1);
            return SetEmployeeRangeAsync(day, day);
        }

        // Ikkala sanani bir yo'la o'rnatadi va jadvalni bir marta yuklaydi
        private Task SetEmployeeRangeAsync(DateTime from, DateTime to)
        {
            _suppressEmployeeReload = true;
            EmployeeFromDate = from;
            EmployeeToDate = to;
            _suppressEmployeeReload = false;
            return LoadEmployeeSalesAsync();
        }

        public async Task LoadEmployeeSalesAsync()
        {
            if (!_currentUser.CanViewAllSales)
            {
                ShowEmployeeSales = false;
                return;
            }

            _employeeCts?.Cancel();
            var cts = _employeeCts = new CancellationTokenSource();
            var ct = cts.Token;

            var from = EmployeeFromDate ?? DateTime.Today;
            var to = EmployeeToDate ?? from;
            if (to < from) (from, to) = (to, from);

            IsEmployeeLoading = true;
            try
            {
                var employees = await _dashboard.GetEmployeeSalesAsync(from, to, ct);
                ct.ThrowIfCancellationRequested();

                var employeesTotal = employees.Sum(x => x.TotalAmount);
                EmployeeSales = new ObservableCollection<DashboardEmployeeRow>(employees.Select(x => new DashboardEmployeeRow
                {
                    Name = x.FullName,
                    Initials = GetInitials(x.FullName),
                    RoleText = x.Role is { } role
                        ? DisplayNames.Of(role) + (x.IsActive ? "" : " • nofaol")
                        : "Xodimga bog‘lanmagan",
                    SalesCount = x.SalesCount,
                    TotalAmount = x.TotalAmount,
                    AverageCheck = x.AverageCheck,
                    CashTotal = x.CashTotal,
                    CardTotal = x.CardTotal,
                    CancelledCount = x.CancelledCount,
                    SharePercent = employeesTotal == 0 ? 0 : 100.0 * x.TotalAmount / employeesTotal
                }));
                HasEmployeeSales = EmployeeSales.Count > 0;
                ShowEmployeeSales = true;
                EmployeeErrorMessage = "";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                EmployeeErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                if (_employeeCts == cts)
                    IsEmployeeLoading = false;
            }
        }

        private (DateTime from, DateTime to) GetRange()
        {
            var today = DateTime.Today;
            return Period switch
            {
                // Hafta dushanbadan boshlanadi
                DashboardPeriod.Week => (today.AddDays(-(((int)today.DayOfWeek + 6) % 7)), today),
                DashboardPeriod.Month => (new DateTime(today.Year, today.Month, 1), today),
                _ => (today, today)
            };
        }

        // "Ali Valiyev" -> "AV"
        private static string GetInitials(string name)
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                0 => "?",
                1 => parts[0][..1].ToUpper(),
                _ => (parts[0][..1] + parts[1][..1]).ToUpper()
            };
        }

        public async Task LoadAsync()
        {
            _cts?.Cancel();
            var cts = _cts = new CancellationTokenSource();
            var ct = cts.Token;

            var (from, to) = GetRange();
            PeriodText = from == to
                ? from.ToString("dd.MM.yyyy")
                : $"{from:dd.MM.yyyy} — {to:dd.MM.yyyy}";
            OnPropertyChanged(nameof(SalesScopeText));

            // Xodimlar jadvali Dashboard davriga moslashadi va o'zi alohida yuklanadi
            _suppressEmployeeReload = true;
            EmployeeFromDate = from;
            EmployeeToDate = to;
            _suppressEmployeeReload = false;
            _ = LoadEmployeeSalesAsync();

            IsLoading = true;
            try
            {
                var summary = await _dashboard.GetDashboardDateAsync(from, to, ct);
                var top = await _dashboard.GetTopProductsAsync(from, to, ct);
                var inventoryEnabled = await _settings.GetInventoryEnabledAsync(ct) == true;
                var warning = inventoryEnabled
                    ? await _dashboard.GetTodayWarningAsync(ct)
                    : null;
                ct.ThrowIfCancellationRequested();

                TotalAmount = summary.TotalAmount;
                SalesCount = summary.SalesCount;
                AverageCheck = summary.AverageCheck;
                CancelledCount = summary.CancelledCount;
                CashCount = summary.CashCount;
                CashTotal = summary.CashTotal;
                CardCount = summary.CardCount;
                CardTotal = summary.CardTotal;
                CashShare = summary.TotalAmount == 0 ? 0 : 100.0 * summary.CashTotal / summary.TotalAmount;
                CardShare = summary.TotalAmount == 0 ? 0 : 100.0 * summary.CardTotal / summary.TotalAmount;

                var maxAmount = top.Count == 0 ? 0 : top.Max(x => x.Amount);
                TopProducts = new ObservableCollection<DashboardTopProductRow>(top.Select((x, i) => new DashboardTopProductRow
                {
                    Rank = i + 1,
                    Name = x.ProductName,
                    QuantityText = $"{NumberInput.Format(x.Quantity)} {DisplayNames.Of(x.Unit)}",
                    Amount = x.Amount,
                    Share = maxAmount == 0 ? 0 : (double)x.Amount / maxAmount
                }));
                HasTopProducts = TopProducts.Count > 0;

                ShowStockWarnings = warning is not null;
                LowStockCount = warning?.LowStockCount ?? 0;
                OutOfStockCount = warning?.OutOfStockCount ?? 0;
                LowStockItems = new ObservableCollection<DashboardLowStockRow>(
                    (warning?.LowStockItems ?? new()).Select(x => new DashboardLowStockRow
                    {
                        Name = x.Name,
                        QuantityText = NumberInput.Format(x.Quantity),
                        MinQuantityText = NumberInput.Format(x.MinQuantity),
                        IsOut = x.Quantity <= 0
                    }));
                HasLowStock = LowStockItems.Count > 0;

                ErrorMessage = "";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                if (_cts == cts)
                    IsLoading = false;
            }
        }
    }
}
