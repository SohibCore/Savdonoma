using System.Windows;
using System.ComponentModel;
using Savdonoma.Core.Enums;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Data.Services.Sales;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Reports;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class ReportsViewModel : ObservableObject
    {
        private readonly IReportService _reports;
        private readonly ISaleService _sales;
        private CancellationTokenSource? _cts;
        private CancellationTokenSource? _detailCts;

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
        [ObservableProperty] private SaleDetailDto? selectedSaleDetail;
        [ObservableProperty] private bool isLoadingSaleDetail;
        [ObservableProperty] private string saleDetailError = "";
        [ObservableProperty] private string errorMessage = "";

        public ReportsViewModel(IReportService reports, ISaleService sales)
        {
            _reports = reports;
            _sales = sales;
        }

        partial void OnFromDateChanged(DateTime? value) => _ = LoadAsync();
        partial void OnToDateChanged(DateTime? value) => _ = LoadAsync();

        public async Task LoadAsync()
        {
            _cts?.Cancel();
            _detailCts?.Cancel();
            var cts = _cts = new CancellationTokenSource();
            var ct = cts.Token;

            var from = FromDate ?? DateTime.Today;
            var to = ToDate ?? from;
            if (to < from) (from, to) = (to, from);

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

                SelectedSaleDetail = null;
                SaleDetailError = "";
                IsLoadingSaleDetail = false;
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
                _ = LoadSaleDetailAsync(sale);
                return;
            }

            if (SelectedSale?.Id == sale.Id)
            {
                _detailCts?.Cancel();
                SelectedSaleDetail = null;
                SaleDetailError = "";
                IsLoadingSaleDetail = false;
            }
        }

        private async Task LoadSaleDetailAsync(SaleRowDto sale)
        {
            _detailCts?.Cancel();
            var cts = _detailCts = new CancellationTokenSource();
            var cancellation = cts.Token;

            SelectedSale = sale;
            foreach (var otherSale in Sales.Where(x => x.Id != sale.Id && x.IsExpanded))
                otherSale.IsExpanded = false;

            SelectedSaleDetail = null;
            SaleDetailError = "";
            IsLoadingSaleDetail = true;

            try
            {
                var details = await _sales.GetDetailAsync(sale.Id, cancellation);
                cancellation.ThrowIfCancellationRequested();
                SelectedSaleDetail = details.FirstOrDefault()
                    ?? throw new InvalidOperationException("Sotuv tafsilotlari topilmadi.");
            }
            catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                SaleDetailError = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                if (ReferenceEquals(_detailCts, cts))
                {
                    IsLoadingSaleDetail = false;
                    _detailCts = null;
                }

                cts.Dispose();
            }
        }

        [RelayCommand] private void ShowToday() => SetRange(DateTime.Today, DateTime.Today);

        [RelayCommand]
        private void ShowWeek()
        {
            var today = DateTime.Today;
            var diff = ((int)today.DayOfWeek + 6) % 7;     // hafta dushanbadan boshlanadi
            SetRange(today.AddDays(-diff), today);
        }

        [RelayCommand]
        private void ShowMonth()
        {
            var today = DateTime.Today;
            SetRange(new DateTime(today.Year, today.Month, 1), today);
        }

        [RelayCommand] private Task Refresh() => LoadAsync();

        private void SetRange(DateTime from, DateTime to)
        {
            FromDate = from;
            ToDate = to;
        }

        [RelayCommand]
        private async Task CancelSaleAsync()
        {
            if (SelectedSale == null) return;

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