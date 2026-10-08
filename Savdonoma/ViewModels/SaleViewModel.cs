using Savdonoma.Core.Enums;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;
using System.ComponentModel;
using Savdonoma.Data.Services.Sales;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Products;
using CommunityToolkit.Mvvm.ComponentModel;
using Savdonoma.Data.Services.Sales.SaleItems;

namespace Savdonoma.ViewModels
{
    public partial class SaleViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly ISaleService _saleService;

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

        public string SessionTitle { get; set; } = "";

        public decimal TotalAmount =>
            CartItems.Sum(x => x.LineTotal);

        public SaleViewModel(
            IProductService productService,
            ISaleService saleService)
        {
            _productService = productService;
            _saleService = saleService;

            _ = LoadProductsAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = LoadProductsAsync();
        }

        partial void OnSelectedProductChanged(ProductDto? value)
        {
            if (value == null)
                return;

            AddToCart(value);

            SelectedProduct = null;
        }

        private async Task LoadProductsAsync()
        {
            _searchCts?.Cancel();

            var cts = _searchCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(200, cts.Token);

                var products = await _productService.SearchAsync(
                    SearchText ?? "",
                    cts.Token,
                    take: 100);
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

        private void AddToCart(ProductDto product)
        {
            ErrorMessage = "";

            var existing = CartItems.FirstOrDefault(
                x => x.ProductId == product.Id);

            if (existing != null)
            {
                existing.Quantity++;
                existing.Refresh();
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
            }

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

            OnPropertyChanged(nameof(TotalAmount));
        }

        [RelayCommand]
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
                        Quantity = (int)x.Quantity
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
                if (!TryParseQuantity(value, out var parsed))
                {
                    // Noto'g'ri qiymat (bo'sh, manfiy, 0 yoki harf) —
                    // eski qiymatni qaytarib ko'rsatamiz.
                    OnPropertyChanged(nameof(QuantityText));
                    return;
                }

                if (parsed != Quantity)
                    Quantity = parsed;

                // Kiritilgan matnni normallashtirish ("1,50" -> "1,5")
                OnPropertyChanged(nameof(QuantityText));
            }
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
                parsed = Math.Round(parsed);

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