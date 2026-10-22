using System.Windows;
using Savdonoma.Services;
using Savdonoma.Core.Enums;
using System.Globalization;
using System.ComponentModel;
using Savdonoma.Core.Printer;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Data.Services.Sales;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Products;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Data.Services.SystemSetting;
using Savdonoma.Data.Services.Sales.SaleItems;

namespace Savdonoma.ViewModels
{
    public partial class SaleViewModel : ObservableObject, IDisposable
    {
        private readonly ILicenseService _license;
        private readonly ISaleService _sale;
        private readonly IProductService _product;
        private readonly ISystemSettingsService _settings;
        private readonly CurrentUserService _currentUser;
        private readonly IReceiptPrinter _printer;

        private CancellationTokenSource? _search;

        [ObservableProperty]
        private ObservableCollection<ProductDto> products = new();
        public ObservableCollection<SaleCartItem> CartItems { get; } = new();

        public Array PaymentMethods { get; } = Enum.GetValues(typeof(PaymentMethod));

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
        private bool isReceivedAmountEntryMode;

        [ObservableProperty]
        private bool isAlphabeticKeyboard;

        [ObservableProperty]
        private string keypadInput = "";

        [ObservableProperty]
        private string receivedAmountText = "";

        public decimal ReceivedAmount => decimal.TryParse(new string(ReceivedAmountText.Where(char.IsDigit).ToArray()), out var v) ? v : 0m;
        public bool IsCash => SelectedPaymentMethod == PaymentMethod.Naqd;
        public decimal Change => Math.Max(0, ReceivedAmount - TotalAmount);
        public decimal RemainingAmount => Math.Max(0, TotalAmount - ReceivedAmount);
        public bool IsUnderpaid => RemainingAmount > 0;

        public string SessionTitle { get; set; } = "";

        public bool CanWrite()
        {
            return _license.Current.IsActive;
        }

        public decimal TotalAmount => CartItems.Sum(x => x.LineTotal);

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _search?.Cancel();
            _search?.Dispose();
            _search = null;
            _license.StatusChanged -= LicenseStatusChanged;
        }

        public SaleViewModel(
            IProductService productService,
            ISaleService saleService,
            ISystemSettingsService settings,
            ILicenseService license,
            CurrentUserService currentUser,
            IReceiptPrinter printer)
        {
            _product = productService;
            _sale = saleService;
            _settings = settings;
            _license = license;
            _currentUser = currentUser;
            _printer = printer;

            _license.StatusChanged += LicenseStatusChanged;
            _ = LoadInventoryModeAsync();
            _ = LoadProductsAsync();
        }

        private async Task LoadProductsAsync() // bazaga qidiruvdagi mahsulotni olib kelish uchun request tayyorlaydi
        {
            _search?.Cancel(); //eskisini bekor qiladi

            var cancel = _search = new CancellationTokenSource(); // yangisini oladi

            try
            {
                await Task.Delay(200, cancel.Token); // 200ms kutib turadi

                // dastlabki 50 ta productni olib keladi
                var products = await _product.SearchAsync(SearchText ?? "", cancel.Token, take: 50);

                cancel.Token.ThrowIfCancellationRequested();

                var selectedProductId = SelectedProduct?.Id;
                SelectedProduct = null;
                Products = new ObservableCollection<ProductDto>(products);
                if (selectedProductId.HasValue)
                    SelectedProduct = Products.FirstOrDefault(x => x.Id == selectedProductId.Value);
            }
            catch (OperationCanceledException ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
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
            ErrorMessage = default!;

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
            IsReceivedAmountEntryMode = false;
            IsAlphabeticKeyboard = false;
            KeypadInput = "";
        }

        [RelayCommand]
        private void SelectSearchMode()
        {
            IsQuantityEntryMode = false;
            IsReceivedAmountEntryMode = false;
            IsAlphabeticKeyboard = false;
            KeypadInput = SearchText;
        }

        [RelayCommand]
        private void SelectReceivedAmountEntryMode()
        {
            IsQuantityEntryMode = false;
            IsReceivedAmountEntryMode = true;
            IsAlphabeticKeyboard = false;
            KeypadInput = ReceivedAmountText;
        }

        [RelayCommand]
        private void ToggleKeyboardLayout()
        {
            if (IsReceivedAmountEntryMode)
                return;

            if (IsQuantityEntryMode)
                SelectSearchMode();

            IsAlphabeticKeyboard = !IsAlphabeticKeyboard;
        }

        [RelayCommand]
        private void AddKey(string key)
        {
            ErrorMessage = "";

            if (IsReceivedAmountEntryMode)
            {
                if (key.Length == 1 && char.IsDigit(key[0]))
                    ReceivedAmountText += key;
                return;
            }

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
            if (IsReceivedAmountEntryMode)
            {
                if (ReceivedAmountText.Length > 0)
                    ReceivedAmountText = ReceivedAmountText[..^1];
                return;
            }

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
            if (IsReceivedAmountEntryMode)
                ReceivedAmountText = "";
            else if (IsQuantityEntryMode)
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

        private bool CanComplete() => CanWrite() && (!IsCash || ReceivedAmount >= TotalAmount);
        [RelayCommand(CanExecute = nameof(CanComplete))]
        private async Task CompleteSaleAsync(CancellationToken cancellation)
        {
            ErrorMessage = "";
            var paid = IsCash ? ReceivedAmount : TotalAmount;

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
                    UserId = _currentUser.UserId,
                    Items = CartItems.Select(x => new CreateSaleItemDto
                    {
                        ProductId = x.ProductId,
                        Quantity = x.Quantity
                    }).ToList()
                };

                var result = await _sale.CreateAsync(dto, CancellationToken.None);

                try
                {
                    var receipt = await _sale.GetReceipt(result.Id, CancellationToken.None);
                    receipt.Paid = paid;

                    var ok = await Task.Run(() => _printer.PrintCheck(receipt, cancellation));
                    if (!ok)
                        ErrorMessage = "Sotuv saqlandi, lekin chek chiqmadi.";
                }
                catch (Exception)
                {
                    ErrorMessage = "Sotuv saqlandi, lekin chek chiqmadi.";
                }

                CartItems.Clear();

                OnPropertyChanged(nameof(TotalAmount));

                await LoadProductsAsync();
                CartItems.Clear();
                ReceivedAmountText = "";
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
                var product = await _product.FindByBarcodeAsync(normalizedCode);
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

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.PropertyName is nameof(TotalAmount) or nameof(ReceivedAmountText)
                or nameof(SelectedPaymentMethod))
            {
                OnPropertyChanged(nameof(ReceivedAmount));
                OnPropertyChanged(nameof(Change));
                OnPropertyChanged(nameof(RemainingAmount));
                OnPropertyChanged(nameof(IsUnderpaid));
                OnPropertyChanged(nameof(IsCash));
                CompleteSaleCommand.NotifyCanExecuteChanged();
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