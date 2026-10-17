using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Core.Entity;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Sales;
using Savdonoma.Data.Services.Sales.SaleItems;
using Savdonoma.Data.Services.SystemSetting;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace Savdonoma.ViewModels
{
    public partial class SaleViewModel : ObservableObject, IDisposable
    {
        private readonly ILicenseService _license;
        private readonly ISaleService _saleService;
        private readonly IProductService _productService;
        private readonly ISystemSettingsService _settings;

        private CancellationTokenSource? _searchCts;

        [ObservableProperty]
        private ObservableCollection<ProductDto> products = new();
        public ObservableCollection<SaleCartItem> CartItems { get; } = new();

        public Array PaymentMethods { get; } =
            Enum.GetValues(typeof(PaymentMethod));

        [ObservableProperty]
        private string searchText = "";

        [ObservableProperty]
        private ProductDto? selectedProduct;

        [ObservableProperty]
        private PaymentMethod selectedPaymentMethod = PaymentMethod.Karta;

        [ObservableProperty]
        private string errorMessage = "";

        [ObservableProperty]
        private bool inventoryEnabled;

        [ObservableProperty]
        private SaleCartItem? selectedCartItem;

        [ObservableProperty]
        private bool isQuantityEntryMode = true;

        [ObservableProperty]
        private bool isAlphabeticKeyboard;

        [ObservableProperty]
        private string keypadInput = "";

        public string SessionTitle { get; set; } = "";
        public bool CanWrite => _license.Current.IsActive;

        public decimal TotalAmount =>
            CartItems.Sum(x => x.LineTotal);

        private async Task LoadProductsAsync()
        {
            _searchCts?.Cancel();

            var cts = _searchCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(200, cts.Token);

                var products = await _productService.SearchAsync(SearchText ?? "", cts.Token, take: 100);
                cts.Token.ThrowIfCancellationRequested();

                var selectedProductId = SelectedProduct?.Id;
                SelectedProduct = null;
                Products = new ObservableCollection<ProductDto>(products);
                if (selectedProductId.HasValue)
                    SelectedProduct = Products.FirstOrDefault(x => x.Id == selectedProductId.Value);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        public SaleViewModel(IProductService productService, ISaleService saleService, ISystemSettingsService settings, ILicenseService license)
        {
            _productService = productService;
            _saleService = saleService;
            _settings = settings;
            _license = license;

            _license.StatusChanged += LicenseStatusChanged;
            _ = LoadInventoryModeAsync();
            _ = LoadProductsAsync();
        }

        private async Task LoadInventoryModeAsync()
        {
            try
            {
                var inventoryEnabled = await _settings.GetInventoryEnabledAsync(CancellationToken.None);
                if (inventoryEnabled is null)
                    throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

                InventoryEnabled = inventoryEnabled.Value;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;
            _license.StatusChanged -= LicenseStatusChanged;
        }

        private void LicenseStatusChanged(object? sender, LicenseState state)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                OnPropertyChanged(nameof(CanWrite));
                CompleteSaleCommand.NotifyCanExecuteChanged();
            }));
        }

        partial void OnSearchTextChanged(string value)
        {
            if (!IsQuantityEntryMode)
                KeypadInput = value;

            _ = LoadProductsAsync();
        }

        partial void OnSelectedProductChanged(ProductDto? value)
        {
            if (value == null)
                return;

            AddToCart(value);

            SelectedProduct = null;
        }

        partial void OnSelectedCartItemChanged(SaleCartItem? value)
        {
            KeypadInput = "";
            if (value != null)
            {
                IsQuantityEntryMode = true;
                IsAlphabeticKeyboard = false;
            }
        }

        partial void OnKeypadInputChanged(string value)
        {
            if (!IsQuantityEntryMode && SearchText != value)
                SearchText = value;
        }

        private void AddToCart(ProductDto product)
        {
            ErrorMessage = "";

            var existing = CartItems.FirstOrDefault(x => x.ProductId == product.Id);

            if (existing != null)
            {
                existing.Quantity++;
                existing.Refresh();
                SelectedCartItem = existing;
            }
            else
            {
                var item = new SaleCartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Unit = product.Unit,
                    Quantity = 1
                };

                // Foydalanuvchi miqdorini qo'lda kiritganda ham
                // JAMI summa yangilanishi uchun.
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName is nameof(SaleCartItem.LineTotal)
                        or nameof(SaleCartItem.Quantity))
                    {
                        OnPropertyChanged(nameof(TotalAmount));
                    }
                };

                CartItems.Add(item);
                SelectedCartItem = item;
            }

            IsQuantityEntryMode = true;
            KeypadInput = "";
            OnPropertyChanged(nameof(TotalAmount));
        }

        [RelayCommand]
        private void IncreaseQuantity(SaleCartItem item)
        {
            item.Quantity++;
            item.Refresh();

            OnPropertyChanged(nameof(TotalAmount));
        }

        [RelayCommand]
        private void DecreaseQuantity(SaleCartItem item)
        {
            if (item.Quantity <= 1)
            {
                CartItems.Remove(item);
                if (SelectedCartItem == item)
                    SelectedCartItem = CartItems.LastOrDefault();
            }
            else
            {
                item.Quantity--;
                item.Refresh();
            }

            OnPropertyChanged(nameof(TotalAmount));
        }

        [RelayCommand]
        private void RemoveItem(SaleCartItem item)
        {
            CartItems.Remove(item);
            if (SelectedCartItem == item)
                SelectedCartItem = CartItems.LastOrDefault();

            OnPropertyChanged(nameof(TotalAmount));
        }

        [RelayCommand]
        private void SelectQuantityEntryMode()
        {
            IsQuantityEntryMode = true;
            IsAlphabeticKeyboard = false;
            KeypadInput = "";
        }

        [RelayCommand]
        private void SelectSearchMode()
        {
            IsQuantityEntryMode = false;
            IsAlphabeticKeyboard = false;
            KeypadInput = SearchText;
        }

        [RelayCommand]
        private void ToggleKeyboardLayout()
        {
            if (IsQuantityEntryMode)
                SelectSearchMode();

            IsAlphabeticKeyboard = !IsAlphabeticKeyboard;
        }

        [RelayCommand]
        private void AddKey(string key)
        {
            ErrorMessage = "";

            if (IsQuantityEntryMode)
            {
                if (SelectedCartItem == null)
                {
                    ErrorMessage = "Miqdor kiritish uchun savatdan mahsulot tanlang.";
                    return;
                }

                if (key == "." && SelectedCartItem.Unit != Unit.Kg)
                    return;

                if (key == "." && KeypadInput.Contains('.'))
                    return;

                if (key.Length != 1 || (!char.IsDigit(key[0]) && key != "."))
                    return;

                KeypadInput += key;
                return;
            }

            SearchText += key;
            KeypadInput = SearchText;
        }

        [RelayCommand]
        private void BackspaceKeypadInput()
        {
            if (IsQuantityEntryMode)
            {
                if (KeypadInput.Length > 0)
                    KeypadInput = KeypadInput[..^1];
                return;
            }

            if (SearchText.Length > 0)
                SearchText = SearchText[..^1];
            KeypadInput = SearchText;
        }

        [RelayCommand]
        private void ClearKeypadInput()
        {
            if (IsQuantityEntryMode)
                KeypadInput = "";
            else
            {
                SearchText = "";
                KeypadInput = "";
            }
        }

        [RelayCommand]
        private void ApplyKeypadInput()
        {
            if (!IsQuantityEntryMode)
                return;

            if (SelectedCartItem == null)
            {
                ErrorMessage = "Miqdor kiritish uchun savatdan mahsulot tanlang.";
                return;
            }

            if (!SelectedCartItem.TrySetQuantity(KeypadInput))
            {
                ErrorMessage = "Miqdorni to'g'ri kiriting.";
                return;
            }

            ErrorMessage = "";
            KeypadInput = "";
            OnPropertyChanged(nameof(TotalAmount));
        }

        [RelayCommand(CanExecute = nameof(CanWrite))]
        private async Task CompleteSaleAsync()
        {
            ErrorMessage = "";

            if (CartItems.Count == 0)
            {
                ErrorMessage = "Savat bo'sh.";
                return;
            }

            try
            {
                var dto = new CreateSaleDto
                {
                    PaymentMethod = SelectedPaymentMethod,
                    Items = CartItems.Select(x => new CreateSaleItemDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity
                    }).ToList()
                };

                var result = await _saleService.CreateAsync(
                    dto,
                    CancellationToken.None);

                CartItems.Clear();

                OnPropertyChanged(nameof(TotalAmount));

                await LoadProductsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage =
                    ex.InnerException?.Message ?? ex.Message;
            }
        }

        public async Task ProcessScannedBarcodeAsync(string code)
        {
            var normalizedCode = code.Trim();
            if (normalizedCode.Length == 0)
            {
                ErrorMessage = "Shtrix-kod bo'sh.";
                return;
            }

            try
            {
                var product = await _productService.FindByBarcodeAsync(normalizedCode);
                if (product == null)
                {
                    ErrorMessage = $"Mahsulot topilmadi: {normalizedCode}";
                    return;
                }

                AddToCart(new ProductDto
                {
                    Id = product.Id,
                    Unit = product.Unit,
                    Name = product.Name,
                    NameSearch = product.NameSearch,
                    Price = product.Price,
                    IsActive = product.IsActive,
                    Barcode = product.Barcode,
                    Quantity = product.Quantity,
                    CategoryId = product.CategoryId
                });
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Shtrix-kodni tekshirishda xatolik: {ex.InnerException?.Message ?? ex.Message}";
            }
        }
    }

    public partial class SaleCartItem : ObservableObject
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";

        public decimal UnitPrice { get; set; }
        public Unit Unit { get; set; }

        [ObservableProperty]
        private decimal quantity;

        public decimal LineTotal =>
            UnitPrice * Quantity;

        /// <summary>
        /// MIQDOR katakchasi uchun matn: kg da sakkannoma (1.25),
        /// boshqa birliklarda butun son (2). Formatlash va parse
        /// model ichida — WPF binding uchun converter talab qilmaydi.
        /// </summary>
        public string QuantityText
        {
            get => Unit == Unit.Kg
                ? Quantity.ToString("0.###")
                : Quantity.ToString("0");

            set
            {
                if (!TrySetQuantity(value))
                    OnPropertyChanged(nameof(QuantityText));
            }
        }

        public bool TrySetQuantity(string? text)
        {
            if (!TryParseQuantity(text, out var parsed))
                return false;

            if (parsed != Quantity)
                Quantity = parsed;

            OnPropertyChanged(nameof(QuantityText));
            return true;
        }

        partial void OnQuantityChanged(decimal value)
        {
            Refresh();
            OnPropertyChanged(nameof(QuantityText));
        }

        private bool TryParseQuantity(string? text, out decimal result)
        {
            result = 0m;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            // "1,5" va "1.5" kiritishlari ikkalasi ham qabul qilinadi
            var normalized = text.Trim().Replace(',', '.');

            if (!decimal.TryParse(
                    normalized,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var parsed))
                return false;

            if (Unit != Unit.Kg)
                parsed = Math.Round(parsed, MidpointRounding.AwayFromZero);

            if (parsed <= 0)
                return false;

            result = parsed;
            return true;
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(LineTotal));
        }
    }
}