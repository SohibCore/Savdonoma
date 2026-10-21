using System.IO;
using Microsoft.Win32;
using System.Windows;
using Savdonoma.Import;
using Savdonoma.Core.Enums;
using System.Globalization;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Products;
using Savdonoma.Data.Services.Categories;
using Savdonoma.Data.Services.SystemSetting;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels
{
    public partial class ProductsViewModel : ObservableObject // UI ga qandaydir o'zgarishlar bo'lsa, xabar beradi
    {
        private readonly IProductService _service;
        private readonly ICategoryService _categoryService;
        private readonly ISystemSettingsService _settings;
        private readonly ILicenseService _license;
        private readonly ProductImportService _importService;
        private CancellationTokenSource? _searchCts;

        public bool CanWrite => _license.Current.IsActive;
        public bool CanSave => CanWrite && InventorySettingsLoaded;
        public bool CanImport => CanWrite && !IsImporting;

        [ObservableProperty] private ObservableCollection<ProductDto> products = new();
        public ObservableCollection<CategroyDto> Categories { get; } = new();
        public Array Units { get; } = Enum.GetValues(typeof(Unit));

        [ObservableProperty] private string searchText = "";
        [ObservableProperty] private ProductDto? selectedProduct;

        // Forma maydonlari
        [ObservableProperty] private int? editingId;      // null = yangi mahsulot
        [ObservableProperty] private string name = "";
        [ObservableProperty] private string priceText = "";
        [ObservableProperty] private string quantityText = "0";
        [ObservableProperty] private bool inventoryEnabled;
        [ObservableProperty] private bool inventorySettingsLoaded;
        [ObservableProperty] private Unit selectedUnit;
        [ObservableProperty] private string barcode = "";
        [ObservableProperty] private string formTitle = "Yangi mahsulot";
        [ObservableProperty] private string formDescription = "Mahsulot ma'lumotlarini kiriting";
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private CategroyDto? selectedCategory;
        [ObservableProperty] private bool isImporting;
        [ObservableProperty] private string importStatus = "";
        [ObservableProperty] private bool isLoadingProducts;

        public bool IsCreating => EditingId is null;
        public bool CanRefreshProducts => !IsLoadingProducts;

        // Bitta konstruktor: uchala servis shu yerda
        public ProductsViewModel(
            IProductService service,
            ICategoryService categoryService,
            ISystemSettingsService settings,
            ILicenseService license,
            ProductImportService importService)
        {
            _service = service;
            _categoryService = categoryService;
            _settings = settings;
            _license = license;
            _importService = importService;

            // Litsenziya holati o'zgarsa (masalan, yarim tunda muddat tugasa), tugmalar yangilanadi.
            // Event fon oqimidan keladi, shuning uchun Dispatcher kerak.
            _license.StatusChanged += (_, _) =>
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    OnPropertyChanged(nameof(CanWrite));
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(CanImport));
                    SaveCommand.NotifyCanExecuteChanged();
                    DeactivateCommand.NotifyCanExecuteChanged();
                    ImportFromExcelCommand.NotifyCanExecuteChanged();
                }));

            _ = LoadAsync();
            _ = LoadCategoriesAsync();
            _ = LoadInventoryModeAsync();
        }

        private async Task LoadInventoryModeAsync()
        {
            try
            {
                var inventoryEnabled = await _settings.GetInventoryEnabledAsync(CancellationToken.None);
                if (inventoryEnabled is null)
                    throw new InvalidOperationException("Qoldiq rejimi hali tanlanmagan.");

                InventoryEnabled = inventoryEnabled.Value;
                InventorySettingsLoaded = true;
                OnPropertyChanged(nameof(CanSave));
                SaveCommand.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
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
            QuantityText = value.Quantity.ToString("0.##", CultureInfo.CurrentCulture);
            SelectedUnit = value.Unit;
            Barcode = value.Barcode ?? "";

            SelectedCategory = Categories
                .FirstOrDefault(x => x.Id == value.CategoryId);

            FormTitle = "Tahrirlash";
            ErrorMessage = "";
        }

        private async Task LoadAsync(bool debounce = true)
        {
            _searchCts?.Cancel();
            var cts = _searchCts = new CancellationTokenSource();
            IsLoadingProducts = true;

            try
            {
                if (debounce)
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
            finally
            {
                cts.Dispose();
                if (ReferenceEquals(_searchCts, cts))
                {
                    _searchCts = null;
                    IsLoadingProducts = false;
                }
            }
        }

        partial void OnIsLoadingProductsChanged(bool value) =>
            RefreshProductsCommand.NotifyCanExecuteChanged();

        [RelayCommand(CanExecute = nameof(CanRefreshProducts))]
        private Task RefreshProductsAsync() => LoadAsync(debounce: false);

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

        partial void OnIsImportingChanged(bool value)
        {
            OnPropertyChanged(nameof(CanImport));
            ImportFromExcelCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand(CanExecute = nameof(CanImport))]
        private async Task ImportFromExcelAsync()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Excel faylini tanlang",
                Filter = "Excel fayllari (*.xlsx;*.xlsm)|*.xlsx;*.xlsm",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            IsImporting = true;
            ImportStatus = "Fayl tekshirilmoqda...";
            ErrorMessage = "";

            try
            {
                var result = await _importService.ValidateAsync(dialog.FileName);
                var summary = FormatImportSummary(result);

                if (result.ReadyCount == 0)
                {
                    MessageBox.Show(summary, "Excel import tekshiruvi",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    ImportStatus = "Importga tayyor mahsulot topilmadi.";
                    return;
                }

                var answer = MessageBox.Show(
                    summary + "\n\nDavom etib, to'g'ri qatorlarni bazaga qo'shasizmi?",
                    "Excel importni tasdiqlash",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (answer != MessageBoxResult.Yes)
                {
                    ImportStatus = "Import bekor qilindi.";
                    return;
                }

                if (!CanWrite)
                {
                    MessageBox.Show(
                        "Litsenziya muddati tugagan. Mahsulotlarni import qilib bo'lmaydi.",
                        "Excel import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    ImportStatus = "Import bajarilmadi.";
                    return;
                }

                await _importService.CommitAsync(result);
                await LoadAsync();

                ImportStatus = $"{result.Added} ta mahsulot import qilindi.";
                MessageBox.Show(
                    $"Import yakunlandi.\nQo'shildi: {result.Added} ta\n" +
                    $"Takror bo'lgani uchun o'tkazildi: {result.Skipped.Count} ta\n" +
                    $"Xato qatorlar: {result.Errors.Count} ta",
                    "Excel import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                ImportStatus = "Importda xatolik yuz berdi.";
                MessageBox.Show(message, "Excel import xatosi",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsImporting = false;
            }
        }

        private static string FormatImportSummary(ProductImportResult result)
        {
            var lines = new List<string>
            {
                $"Importga tayyor: {result.ReadyCount} ta",
                $"Takror bo'lgani uchun o'tkaziladi: {result.Skipped.Count} ta",
                $"Xato qatorlar: {result.Errors.Count} ta"
            };

            AddDetails(lines, "Takrorlar", result.Skipped);
            AddDetails(lines, "Xatolar", result.Errors);

            return "Excel fayl tekshiruv natijasi:\n\n" + string.Join("\n", lines);
        }

        private static void AddDetails(List<string> lines, string title, List<string> entries)
        {
            if (entries.Count == 0)
                return;

            lines.Add($"\n{title}:");
            lines.AddRange(entries.Take(8).Select(entry => $"- {entry}"));

            if (entries.Count > 8)
                lines.Add($"- ... yana {entries.Count - 8} ta");
        }

        [RelayCommand]
        private void DownloadExcelTemplate()
        {
            var dialog = new SaveFileDialog
            {
                Title = "Excel shablonini saqlash",
                FileName = "TijoratUz mahsulot import shabloni.xlsx",
                Filter = "Excel fayli (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                OverwritePrompt = true
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var resourceUri = new Uri(
                    "pack://application:,,,/TijoratUz;component/wwwroot/default%20import%20file.xlsx",
                    UriKind.Absolute);
                var resource = Application.GetResourceStream(resourceUri)
                    ?? throw new FileNotFoundException("Excel shabloni ilova resurslaridan topilmadi.");

                using (resource.Stream)
                using (var output = new FileStream(dialog.FileName, FileMode.Create, FileAccess.Write))
                    resource.Stream.CopyTo(output);

                ImportStatus = $"Shablon saqlandi: {dialog.FileName}";
            }
            catch (Exception ex)
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                MessageBox.Show(message, "Excel shablonini saqlashda xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void NewProduct()
        {
            SelectedProduct = null;
            EditingId = null;

            Name = "";
            PriceText = "";
            QuantityText = "0";
            Barcode = "";
            SelectedCategory = null;

            FormTitle = "Yangi mahsulot";
            ErrorMessage = "";
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private async Task SaveAsync()
        {
            ErrorMessage = "";

            var digits = new string(PriceText.Where(char.IsDigit).ToArray());
            if (!long.TryParse(digits, out var price))
            {
                ErrorMessage = "Narx noto'g'ri kiritilgan";
                return;
            }

            var quantity = 0m;
            if (InventoryEnabled
                && (!decimal.TryParse(QuantityText, NumberStyles.Number, CultureInfo.CurrentCulture, out quantity)
                    || quantity < 0))
            {
                ErrorMessage = "Mahsulot qoldig'i 0 yoki undan katta bo'lishi kerak";
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
                        CategoryId = category.Id,
                        Quantity = quantity
                    }, CancellationToken.None);
                }
                else
                {
                    await _service.UpdateAsync(new UpdateProductDto
                    {
                        Id = EditingId.Value,
                        Price = price,
                        Quantity = quantity
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
                "TijoratUz", MessageBoxButton.YesNo, MessageBoxImage.Question);
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