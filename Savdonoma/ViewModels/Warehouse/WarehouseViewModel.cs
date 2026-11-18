using System.Windows;
using Savdonoma.Services;
using Savdonoma.Converters;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Views.Warehouse;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Data.Services.Products;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace Savdonoma.ViewModels.Warehouse
{
    // Ombor bo'limi: hujjatlar, kam qolganlar, yetkazib beruvchilar, narx sozlamalari
    public partial class WarehouseViewModel : ObservableObject
    {
        private readonly IServiceProvider _sp;
        private readonly IStockService _stock;
        private readonly IProductService _products;
        private readonly CurrentUserService _currentUser;
        private readonly ILicenseService _license;

        public SuppliersViewModel Suppliers { get; }
        public PricingSettingsViewModel Pricing { get; }

        public bool IsOwner => _currentUser.IsOwner;
        public bool CanWrite => _license.Current.IsActive;

        // ---------- Hujjatlar ----------

        public record FilterOption<T>(T? Value, string Name) where T : struct;

        public IReadOnlyList<FilterOption<StockDocumentType>> TypeFilters { get; } = new[]
        {
            new FilterOption<StockDocumentType>(null, "Barcha turlar"),
            new FilterOption<StockDocumentType>(StockDocumentType.Receipt, "Kirim"),
            new FilterOption<StockDocumentType>(StockDocumentType.WriteOff, "Hisobdan chiqarish"),
            new FilterOption<StockDocumentType>(StockDocumentType.Inventory, "Inventarizatsiya"),
        };

        public IReadOnlyList<FilterOption<StockDocumentStatus>> StatusFilters { get; } = new[]
        {
            new FilterOption<StockDocumentStatus>(null, "Barcha holatlar"),
            new FilterOption<StockDocumentStatus>(StockDocumentStatus.Draft, "Qoralama"),
            new FilterOption<StockDocumentStatus>(StockDocumentStatus.Posted, "O'tkazilgan"),
            new FilterOption<StockDocumentStatus>(StockDocumentStatus.Cancelled, "Bekor qilingan"),
        };

        [ObservableProperty] private FilterOption<StockDocumentType> selectedTypeFilter;
        [ObservableProperty] private FilterOption<StockDocumentStatus> selectedStatusFilter;
        [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddDays(-30);
        [ObservableProperty] private DateTime? toDate = DateTime.Today;

        public ObservableCollection<StockDocumentListItemDto> Documents { get; } = new();
        [ObservableProperty] private StockDocumentListItemDto? selectedDocument;

        // ---------- Kam qolganlar ----------

        public ObservableCollection<LowStockDto> LowStock { get; } = new();
        [ObservableProperty] private string lowStockSummary = "";

        [ObservableProperty] private string errorMessage = "";

        public WarehouseViewModel(
            IServiceProvider sp,
            IStockService stock,
            IProductService products,
            CurrentUserService currentUser,
            ILicenseService license,
            SuppliersViewModel suppliers,
            PricingSettingsViewModel pricing)
        {
            _sp = sp;
            _stock = stock;
            _products = products;
            _currentUser = currentUser;
            _license = license;
            Suppliers = suppliers;
            Pricing = pricing;

            selectedTypeFilter = TypeFilters[0];
            selectedStatusFilter = StatusFilters[0];
        }

        public async Task LoadAsync()
        {
            await LoadDocumentsAsync();
            await LoadLowStockAsync();
            await Suppliers.LoadAsync();
            if (IsOwner)
                await Pricing.LoadAsync();
        }

        partial void OnSelectedTypeFilterChanged(FilterOption<StockDocumentType> value) => _ = LoadDocumentsAsync();
        partial void OnSelectedStatusFilterChanged(FilterOption<StockDocumentStatus> value) => _ = LoadDocumentsAsync();
        partial void OnFromDateChanged(DateTime? value) => _ = LoadDocumentsAsync();
        partial void OnToDateChanged(DateTime? value) => _ = LoadDocumentsAsync();

        [RelayCommand]
        private async Task LoadDocumentsAsync()
        {
            try
            {
                var items = await _stock.GetListAsync(new StockDocumentFilter
                {
                    Type = SelectedTypeFilter?.Value,
                    Status = SelectedStatusFilter?.Value,
                    From = FromDate,
                    To = ToDate,
                }, CancellationToken.None);

                Documents.Clear();
                foreach (var item in items)
                    Documents.Add(item);
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        [RelayCommand]
        private async Task LoadLowStockAsync()
        {
            try
            {
                var items = await _stock.GetLowStockAsync(CancellationToken.None);
                LowStock.Clear();
                foreach (var item in items)
                    LowStock.Add(item);

                LowStockSummary = items.Count == 0
                    ? "Minimal qoldiqdan kam tovar yo'q. Minimal qoldiq mahsulot kartasida belgilanadi."
                    : $"{items.Count} ta mahsulot minimal qoldiqdan kam yoki teng.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        // ---------- Hujjat ochish ----------

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private Task NewReceiptAsync() => OpenNewAsync(StockDocumentType.Receipt);

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private Task NewWriteOffAsync() => OpenNewAsync(StockDocumentType.WriteOff);

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private Task NewInventoryAsync() => OpenNewAsync(StockDocumentType.Inventory);

        // Kam qolganlardan kirim: miqdor minimal qoldiqqa yetkazadigan qilib to'ldiriladi
        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task ReceiptFromLowStockAsync()
        {
            if (LowStock.Count == 0)
                return;

            var prefill = new List<(ProductDto, decimal)>();
            foreach (var item in LowStock)
            {
                var product = await _products.GetAsync(item.ProductId, CancellationToken.None);
                var need = Math.Max(1, Math.Ceiling(item.MinQuantity - item.Quantity));
                prefill.Add((product, need));
            }

            await OpenNewAsync(StockDocumentType.Receipt, prefill);
        }

        private async Task OpenNewAsync(StockDocumentType type, IEnumerable<(ProductDto, decimal)>? prefill = null)
        {
            try
            {
                var window = CreateWindow();
                await window.ViewModel.InitializeNewAsync(type, prefill);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        [RelayCommand]
        private async Task OpenDocumentAsync(StockDocumentListItemDto? document)
        {
            document ??= SelectedDocument;
            if (document is null)
                return;

            try
            {
                var window = CreateWindow();
                await window.ViewModel.LoadAsync(document.Id);
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        private StockDocumentWindow CreateWindow()
        {
            var window = _sp.GetRequiredService<StockDocumentWindow>();
            window.Owner = Application.Current.MainWindow;
            window.ViewModel.DocumentChanged += () => _ = RefreshAfterChangeAsync();
            return window;
        }

        private async Task RefreshAfterChangeAsync()
        {
            await LoadDocumentsAsync();
            await LoadLowStockAsync();
            await Suppliers.LoadAsync(); // kirim yetkazib beruvchi qarzini o'zgartiradi
        }

        [RelayCommand]
        private void ShowProductHistory(LowStockDto? item)
        {
            if (item is null)
                return;
            ProductHistoryWindow.Open(_sp, item.ProductId, item.Name);
        }

        public static string TypeName(StockDocumentType type) => DisplayNames.Of(type);
    }
}
