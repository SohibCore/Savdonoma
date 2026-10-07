using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Core.Enums;
using Savdonoma.Data.Services.Categories;
using Savdonoma.Data.Services.Products;
using System.Collections.ObjectModel;
using System.Windows;

namespace Savdonoma.ViewModels
{
    public partial class ProductsViewModel : ObservableObject // UI ga qandaydir o'zgarishlar bo'lsa, xabar beradi
    {
        private readonly IProductService _service;
        private readonly ICategoryService _categoryService;
        private CancellationTokenSource? _searchCts;

        [ObservableProperty] private ObservableCollection<ProductDto> products = new();
        public ObservableCollection<CategroyDto> Categories { get; } = new();
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
        [ObservableProperty] private CategroyDto? selectedCategory;

        public ProductsViewModel(IProductService service, ICategoryService categoryService)
        {
            _service = service;
            _categoryService = categoryService;
            _ = LoadAsync();
            _ = LoadCategoriesAsync();
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

            SelectedCategory = Categories
                .FirstOrDefault(x => x.Id == value.CategoryId);

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
                cts.Token.ThrowIfCancellationRequested();

                var selectedProductId = SelectedProduct?.Id;
                SelectedProduct = null;
                Products = new ObservableCollection<ProductDto>(items);
                if (selectedProductId.HasValue)
                    SelectedProduct = Products.FirstOrDefault(x => x.Id == selectedProductId.Value);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }
        #region
        [RelayCommand]
        private void NewProduct()
        {
            SelectedProduct = null;
            EditingId = null;

            Name = "";
            PriceText = "";
            Barcode = "";

            SelectedCategory = null;

            FormTitle = "Yangi mahsulot";
            ErrorMessage = "";
        }
        #endregion

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
                    var category = SelectedCategory;
                    if (category is null || category.Id <= 0)
                    {
                        ErrorMessage = "Kategoriya tanlang";
                        return;
                    }

                    await _service.CreateAsync(new CreateProductDto
                    {
                        Name = Name,
                        Unit = SelectedUnit,
                        Price = price,
                        Barcode = Barcode,
                        CategoryId = category.Id
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
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryService.GetListAsync(
                    CancellationToken.None);

                Categories.Clear();

                foreach (var category in categories)
                {
                    Categories.Add(category);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }
    }
}