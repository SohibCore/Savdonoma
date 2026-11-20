using System.Windows;
using System.ComponentModel;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Printer;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Data.Services.Sales;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Reports;
using Savdonoma.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Views;

namespace Savdonoma.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly IReportService _reports;
        private readonly ISaleService _sales;
        private readonly IReceiptPrinter _printer;
        private readonly CurrentUserService _currentUser;
        private CancellationTokenSource? _cts;

        // ViewModel singleton: foydalanuvchi almashishi mumkin, shuning uchun har safar joriy foydalanuvchidan olinadi
        public bool CanCancelSales => _currentUser.CanCancelSales;
        public string SalesSubtitle => _currentUser.CanViewAllSales
            ? "Tanlangan sana oralig'idagi savdolar"
            : "Tanlangan sana oralig'idagi sizning savdolaringiz";

        [ObservableProperty] private ObservableCollection<SaleRowDto> sales = new();

        [ObservableProperty] private DateTime? fromDate = DateTime.Today;
        [ObservableProperty] private DateTime? toDate = DateTime.Today;

        [ObservableProperty] private long totalAmount;
        [ObservableProperty] private long cashAmount;
        [ObservableProperty] private long cardAmount;
        [ObservableProperty] private long averageCheck;
        [ObservableProperty] private int salesCount;
        [ObservableProperty] private int cancelledCount;

        [ObservableProperty] private SaleRowDto? selectedSale;
        [ObservableProperty] private string errorMessage = "";

        public ReportsViewModel(
            IReportService reports,
            ISaleService sales,
            IReceiptPrinter printer,
            CurrentUserService currentUser)
        {
            _reports = reports;
            _sales = sales;
            _printer = printer;
            _currentUser = currentUser;
        }

        partial void OnFromDateChanged(DateTime? value) { NotifyRangeButtons(); _ = LoadAsync(); }
        partial void OnToDateChanged(DateTime? value) { NotifyRangeButtons(); _ = LoadAsync(); }

        // Bugun / Shu hafta / Shu oy tugmalaridan qaysi biri tanlangani (sanalar qo'lda tanlansa ham mos keladi)
        private static DateTime WeekStart => DateTime.Today.AddDays(-(((int)DateTime.Today.DayOfWeek + 6) % 7));
        private static DateTime MonthStart => new(DateTime.Today.Year, DateTime.Today.Month, 1);
        public bool IsTodayRange => FromDate == DateTime.Today && ToDate == DateTime.Today;
        public bool IsWeekRange => FromDate == WeekStart && ToDate == DateTime.Today && !IsTodayRange;
        public bool IsMonthRange => FromDate == MonthStart && ToDate == DateTime.Today && !IsTodayRange && !IsWeekRange;

        private void NotifyRangeButtons()
        {
            OnPropertyChanged(nameof(IsTodayRange));
            OnPropertyChanged(nameof(IsWeekRange));
            OnPropertyChanged(nameof(IsMonthRange));
        }

        public async Task LoadAsync()
        {
            _cts?.Cancel();
            var cts = _cts = new CancellationTokenSource();
            var ct = cts.Token;

            var from = FromDate ?? DateTime.Today;
            var to = ToDate ?? from;
            if (to < from) (from, to) = (to, from);

            OnPropertyChanged(nameof(CanCancelSales));
            OnPropertyChanged(nameof(SalesSubtitle));
            CancelSaleCommand.NotifyCanExecuteChanged();

            try
            {
                var summary = await _reports.GetSummaryAsync(from, to, ct);
                var rows = await _reports.GetSalesAsync(from, to, ct);
                ct.ThrowIfCancellationRequested();

                TotalAmount = summary.TotalAmount;
                CashAmount = summary.CashAmount;
                CardAmount = summary.CardAmount;
                AverageCheck = summary.AverageCheck;
                SalesCount = summary.SalesCount;
                CancelledCount = summary.CancelledCount;

                var selectedSaleId = SelectedSale?.Id;
                SelectedSale = null;
                foreach (var sale in Sales)
                    sale.PropertyChanged -= SaleRow_PropertyChanged;
                Sales = new ObservableCollection<SaleRowDto>(rows);
                foreach (var sale in Sales)
                    sale.PropertyChanged += SaleRow_PropertyChanged;
                if (selectedSaleId.HasValue)
                    SelectedSale = Sales.FirstOrDefault(x => x.Id == selectedSaleId.Value);

                ErrorMessage = "";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private void SaleRow_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(SaleRowDto.IsExpanded)
                || sender is not SaleRowDto sale)
                return;

            if (sale.IsExpanded)
            {
                SelectedSale = sale;
                foreach (var otherSale in Sales.Where(x => x.Id != sale.Id && x.IsExpanded))
                    otherSale.IsExpanded = false;

                if (sale.Detail == null && !sale.IsLoadingDetail)
                    _ = LoadSaleDetailAsync(sale);
            }
        }

        private async Task LoadSaleDetailAsync(SaleRowDto sale)
        {
            sale.IsLoadingDetail = true;
            sale.DetailError = "";

            try
            {
                var details = await _sales.GetDetailAsync(sale.Id, CancellationToken.None);
                sale.Detail = details.FirstOrDefault()
                    ?? throw new InvalidOperationException("Sotuv tafsilotlari topilmadi.");
            }
            catch (Exception ex)
            {
                sale.DetailError = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                sale.IsLoadingDetail = false;
            }
        }

        [RelayCommand]
        private async Task ShowReceiptAsync(SaleRowDto? sale)
        {
            if (sale is null)
                return;

            ErrorMessage = "";
            try
            {
                var receipt = await _sales.GetReceipt(sale.Id, CancellationToken.None);
                var window = new ReceiptPreviewWindow(receipt, _printer);
                if (Application.Current.MainWindow is { } owner)
                    window.Owner = owner;
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        [RelayCommand] private void ShowToday() => SetRange(DateTime.Today, DateTime.Today);

        [RelayCommand]
        private void ShowWeek()
        {
            var today = DateTime.Today;
            SetRange(WeekStart, today);                     // hafta dushanbadan boshlanadi
        }

        [RelayCommand]
        private void ShowMonth()
        {
            SetRange(MonthStart, DateTime.Today);
        }

        [RelayCommand] private Task Refresh() => LoadAsync();

        private void SetRange(DateTime from, DateTime to)
        {
            FromDate = from;
            ToDate = to;
        }

        [RelayCommand(CanExecute = nameof(CanCancelSales))]
        private async Task CancelSaleAsync()
        {
            if (SelectedSale == null || !CanCancelSales) return;

            if (SelectedSale.Status == SaleStatus.Cancelled)
            {
                ErrorMessage = "Bu chek allaqachon bekor qilingan";
                return;
            }

            var answer = MessageBox.Show($"{SelectedSale.Number} chekni bekor qilamizmi?",
                "TijoratUz", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                await _sales.CancelAsync(SelectedSale.Id, null, CancellationToken.None);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }
    }
}