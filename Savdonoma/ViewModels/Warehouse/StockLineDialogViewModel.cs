using Savdonoma.Converters;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Products;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels.Warehouse
{
    // Hujjatga mahsulot qo'shish / qatorni tahrirlash oynasi.
    // 1-qadam: mahsulotni tanlash. 2-qadam: miqdor va narxlarni kiritish.
    public partial class StockLineDialogViewModel : ObservableObject
    {
        private readonly StockDocumentEditorViewModel _document;
        private readonly bool _isEditingExisting;
        private CancellationTokenSource? _searchCts;

        public ObservableCollection<ProductDto> Products { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsSelecting), nameof(IsFilling), nameof(Title), nameof(ConfirmText))]
        private StockLineViewModel? line;

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private ProductDto? selectedProduct;
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private string searchInfo = "";

        // Oyna yopilishi kerak: true = qo'shildi/saqlandi
        public event Action<bool>? Finished;

        public StockLineDialogViewModel(StockDocumentEditorViewModel document, StockLineViewModel? existing)
        {
            _document = document;
            _isEditingExisting = existing is not null;
            line = existing?.Clone();
        }

        public bool IsSelecting => Line is null;
        public bool IsFilling => Line is not null;
        public bool CanChangeProduct => !_isEditingExisting;

        public string Title => Line is null
            ? (_document.IsInventory ? "Qaysi mahsulotni sanaysiz?" : "Mahsulotni tanlang")
            : Line.ProductName;

        public string ConfirmText => _isEditingExisting ? "Saqlash" : "Hujjatga qo'shish";

        public string QuantityLabel => _document.IsInventory
            ? $"Haqiqatda sanalgan miqdor ({Line?.UnitText})"
            : _document.IsWriteOff
                ? $"Hisobdan chiqariladigan miqdor ({Line?.UnitText})"
                : $"Kelgan miqdor ({Line?.UnitText})";

        public string CostLabel => $"Kirim narxi, 1 {Line?.UnitText} uchun (so'm)";

        partial void OnLineChanged(StockLineViewModel? value)
        {
            OnPropertyChanged(nameof(QuantityLabel));
            OnPropertyChanged(nameof(CostLabel));
        }

        public async Task InitializeAsync()
        {
            if (IsSelecting)
                await SearchAsync("", debounce: false);
        }

        partial void OnSearchTextChanged(string value) => _ = SearchAsync(value);

        private async Task SearchAsync(string text, bool debounce = true)
        {
            _searchCts?.Cancel();
            var cts = _searchCts = new CancellationTokenSource();

            try
            {
                if (debounce)
                    await Task.Delay(200, cts.Token);
                var items = await _document.SearchProductsAsync(text, cts.Token);
                cts.Token.ThrowIfCancellationRequested();

                Products.Clear();
                foreach (var item in items)
                    Products.Add(item);

                SelectedProduct = Products.FirstOrDefault();
                SearchInfo = Products.Count == 0
                    ? "Mahsulot topilmadi. Yangi mahsulotni avval \"Mahsulotlar\" bo'limida qo'shing."
                    : $"{Products.Count} ta mahsulot. Tanlash uchun ikki marta bosing yoki Enter.";
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        // Qidiruvda Enter: shtrix-kod bo'lsa o'sha mahsulot, aks holda ro'yxatda tanlangani
        [RelayCommand]
        private async Task SubmitSearchAsync()
        {
            var text = SearchText.Trim();
            if (text.Length > 0)
            {
                try
                {
                    var found = await _document.ResolveBarcodeAsync(text);
                    if (found is not null)
                    {
                        Choose(found.Product, found.Quantity);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.InnerException?.Message ?? ex.Message;
                    return;
                }
            }

            if (SelectedProduct is not null)
                Choose(SelectedProduct, null);
        }

        [RelayCommand]
        private void ChooseProduct(ProductDto? product)
        {
            if (product is not null)
                Choose(product, null);
        }

        // Mahsulot tanlandi: 2-qadamga o'tamiz.
        // Hujjatda allaqachon bo'lsa, o'sha qator tahrirlanadi (kirimda miqdori qo'shiladi).
        public void Choose(ProductDto product, decimal? quantity)
        {
            ErrorMessage = "";
            var existing = _document.FindLine(product.Id);
            if (existing is not null)
            {
                var copy = existing.Clone();
                if (!_document.IsInventory)
                    copy.QuantityText = NumberInput.Format((copy.Quantity ?? 0) + (quantity ?? 1));
                Line = copy;
                ErrorMessage = "Bu mahsulot hujjatda bor edi: miqdori qo'shildi, tekshirib saqlang.";
                return;
            }

            Line = _document.CreateLine(product, quantity);
        }

        [RelayCommand]
        private void BackToSelection()
        {
            if (!CanChangeProduct)
                return;
            Line = null;
            ErrorMessage = "";
        }

        [RelayCommand]
        private void ResetPrice() => Line?.ResetToSuggestion();

        [RelayCommand]
        private void Confirm()
        {
            if (Line is null)
            {
                ErrorMessage = "Avval mahsulotni tanlang.";
                return;
            }

            var error = Line.Validate();
            if (error is not null)
            {
                ErrorMessage = error;
                return;
            }

            Finished?.Invoke(true);
        }
    }
}
