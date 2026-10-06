using Savdonoma.Core.Enums;
using CommunityToolkit.Mvvm.Input;
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
        private PaymentMethod selectedPaymentMethod;

        [ObservableProperty]
        private string errorMessage = "";

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
                CartItems.Add(new SaleCartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = 1
                });
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
    }

    public partial class SaleCartItem : ObservableObject
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public decimal UnitPrice { get; set; }

        [ObservableProperty]
        private int quantity;

        public decimal LineTotal =>
            UnitPrice * Quantity;

        public void Refresh()
        {
            OnPropertyChanged(nameof(LineTotal));
        }
    }
}