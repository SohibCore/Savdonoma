using System.Windows;
using Savdonoma.Core.Enums;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Products;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class ProductsViewModel : ObservableObject
    {
        private readonly IProductService _service;
        private CancellationTokenSource? _searchCts;

        public ObservableCollection<ProductDto> Products { get; } = new();
        public Array Units { get; } = Enum.GetValues(typeof(Unit));

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private ProductDto? selectedProduct;

        // Forma maydonlari
        [ObservableProperty] private int? editingId;      // null = yangi mahsulot
        [ObservableProperty] private string name = "";
        [ObservableProperty] private string priceText = "";
        [ObservableProperty] private Unit selectedUnit;
        [ObservableProperty] private string barcode = "";
        [ObservableProperty] private string formTitle = "Yangi mahsulot";
        [ObservableProperty] private string errorMessage = "";

        public ProductsViewModel(IProductService service)
        {
            _service = service;
            _ = LoadAsync();
        }

        // Qidirish matni o'zgarganda ro'yxat yangilanadi
        partial void OnSearchTextChanged(string value) => _ = LoadAsync();

        // Ro'yxatdan mahsulot tanlanganda forma to'ladi
        partial void OnSelectedProductChanged(ProductDto? value)
        {
            if (value == null) return;

            EditingId = value.Id;
            Name = value.Name;
            PriceText = value.Price.ToString();
            SelectedUnit = value.Unit;
            Barcode = value.Barcode ?? "";
            FormTitle = "Tahrirlash";
            ErrorMessage = "";
        }

        private async Task LoadAsync()
        {
            _searchCts?.Cancel();
            var cts = _searchCts = new CancellationTokenSource();

            try
            {
                await Task.Delay(200, cts.Token);   // har harfda emas, yozish to'xtagach qidiradi
                var items = await _service.SearchAsync(SearchText ?? "", cts.Token, take: 1000);

                Products.Clear();
                foreach (var p in items)
                    Products.Add(p);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        [RelayCommand]
        private void NewProduct()
        {
            SelectedProduct = null;
            EditingId = null;
            Name = "";
            PriceText = "";
            Barcode = "";
            FormTitle = "Yangi mahsulot";
            ErrorMessage = "";
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            ErrorMessage = "";

            var digits = new string(PriceText.Where(char.IsDigit).ToArray());
            if (!long.TryParse(digits, out var price))
            {
                ErrorMessage = "Narx noto'g'ri kiritilgan";
                return;
            }

            try
            {
                if (EditingId == null)
                {
                    await _service.CreateAsync(new CreateProductDto
                    {
                        Name = Name,
                        Unit = SelectedUnit,
                        Price = price,
                        Barcode = Barcode
                    }, CancellationToken.None);
                }
                else
                {
                    await _service.UpdateAsync(new UpdateProductDto
                    {
                        Id = EditingId.Value,
                        Name = Name,
                        Unit = SelectedUnit,
                        Price = price,
                        Barcode = Barcode
                    }, CancellationToken.None);
                }

                NewProduct();
                await LoadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Xatolik",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                ErrorMessage = ex.Message;
            }
        }

        [RelayCommand]
        private async Task DeactivateAsync()
        {
            if (EditingId == null) return;

            var answer = MessageBox.Show("Bu mahsulotni ro'yxatdan olib tashlaymizmi?",
                "Savdonoma", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (answer != MessageBoxResult.Yes) return;

            try
            {
                await _service.DeactivateAsync(EditingId.Value, CancellationToken.None);
                NewProduct();
                await LoadAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }
    }
}