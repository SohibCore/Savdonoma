using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
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
        private readonly ILicenseService _license;
        private CancellationTokenSource? _searchCts;

        public bool CanWrite => _license.Current.IsActive;

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
        [ObservableProperty] private string formDescription = "Mahsulot ma'lumotlarini kiriting";
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private CategroyDto? selectedCategory;

        public bool IsCreating => EditingId is null;

        // Bitta konstruktor: uchala servis shu yerda
        public ProductsViewModel(
            IProductService service,
            ICategoryService categoryService,
            ILicenseService license)
        {
            _service = service;
            _categoryService = categoryService;
            _license = license;

            // Litsenziya holati o'zgarsa (masalan, yarim tunda muddat tugasa), tugmalar yangilanadi.
            // Event fon oqimidan keladi, shuning uchun Dispatcher kerak.
            _license.StatusChanged += (_, _) =>
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged(nameof(CanWrite));
                    SaveCommand.NotifyCanExecuteChanged();
                    DeactivateCommand.NotifyCanExecuteChanged();
                }));

            _ = LoadAsync();
            _ = LoadCategoriesAsync();
        }

        partial void OnEditingIdChanged(int? value)
        {
            OnPropertyChanged(nameof(IsCreating));
            FormDescription = value.HasValue
                ? "Tahrirlashda faqat narxni o'zgartirish mumkin"
                : "Mahsulot ma'lumotlarini kiriting";
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

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _categoryService.GetListAsync(CancellationToken.None);

                Categories.Clear();
                foreach (var category in categories)
                    Categories.Add(category);
            }
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
            SelectedCategory = null;

            FormTitle = "Yangi mahsulot";
            ErrorMessage = "";
        }

        [RelayCommand(CanExecute = nameof(CanWrite))]
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
                        Price = price
                    }, CancellationToken.None);
                }

                NewProduct();
                await LoadAsync();
            }
            catch (LicenseExpiredException ex)
            {
                ErrorMessage = ex.Message;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        [RelayCommand(CanExecute = nameof(CanWrite))]
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