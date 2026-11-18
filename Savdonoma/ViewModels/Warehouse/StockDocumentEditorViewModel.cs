using System.Windows;
using Savdonoma.Services;
using Savdonoma.Converters;
using Savdonoma.Core.Enums;
using Savdonoma.Core.Licensing;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Stock;
using Savdonoma.Data.Services.Pricing;
using Savdonoma.Data.Services.Products;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels.Warehouse
{
    // Kirim / hisobdan chiqarish / inventarizatsiya hujjatini yaratish va tahrirlash.
    // Mahsulot qo'shish va qatorni tahrirlash alohida oynada (StockLineDialog) bo'ladi.
    public partial class StockDocumentEditorViewModel : ObservableObject
    {
        private readonly IStockService _stock;
        private readonly IProductService _products;
        private readonly ISupplierService _suppliers;
        private readonly IPricingService _pricing;
        private readonly CurrentUserService _currentUser;
        private readonly ILicenseService _license;
        private PricingRules? _rules;

        public ObservableCollection<StockLineViewModel> Lines { get; } = new();
        public ObservableCollection<SupplierDto> Suppliers { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title), nameof(IsReceipt), nameof(IsWriteOff), nameof(IsInventory), nameof(Hint), nameof(TotalLabel), nameof(AddButtonText))]
        private StockDocumentType type;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(StatusText), nameof(CanDeleteDraft))]
        [NotifyCanExecuteChangedFor(nameof(DeleteDraftCommand))]
        private int? documentId;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Title))]
        private string number = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditable), nameof(IsReadOnly), nameof(StatusText), nameof(CanCancelDocument), nameof(CanDeleteDraft), nameof(ShowEmptyHint))]
        [NotifyCanExecuteChangedFor(nameof(SaveDraftCommand), nameof(PostCommand), nameof(DeleteDraftCommand), nameof(CancelDocumentCommand))]
        private StockDocumentStatus status = StockDocumentStatus.Draft;

        [ObservableProperty] private SupplierDto? selectedSupplier;
        [ObservableProperty] private string note = "";
        [ObservableProperty] private string scanText = "";
        [ObservableProperty] private string errorMessage = "";
        [ObservableProperty] private string infoText = "";
        [ObservableProperty] private string cancelReason = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveDraftCommand), nameof(PostCommand), nameof(DeleteDraftCommand), nameof(CancelDocumentCommand))]
        private bool isBusy;

        // Hujjat bazada o'zgarganda (ro'yxatni yangilash uchun)
        public event Action? DocumentChanged;
        public event Action? CloseRequested;

        // Shtrix-kod skanerlanganda: oyna mahsulot oynasini shu mahsulot bilan ochadi
        public event Action<ProductDto, decimal?>? ProductScanned;

        public StockDocumentEditorViewModel(
            IStockService stock,
            IProductService products,
            ISupplierService suppliers,
            IPricingService pricing,
            CurrentUserService currentUser,
            ILicenseService license)
        {
            _stock = stock;
            _products = products;
            _suppliers = suppliers;
            _pricing = pricing;
            _currentUser = currentUser;
            _license = license;
            Lines.CollectionChanged += (_, e) =>
            {
                if (e.NewItems is not null)
                    foreach (StockLineViewModel line in e.NewItems)
                        line.PropertyChanged += (_, _) => OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(TotalText));
                OnPropertyChanged(nameof(LineCountText));
                OnPropertyChanged(nameof(ShowEmptyHint));
                OnPropertyChanged(nameof(HasLines));
            };
        }

        public string Title => string.IsNullOrEmpty(Number)
            ? $"Yangi hujjat: {DisplayNames.Of(Type)}"
            : $"{DisplayNames.Of(Type)} {Number}";

        public string StatusText => DocumentId is null ? "Yangi" : DisplayNames.Of(Status);
        public bool IsReceipt => Type == StockDocumentType.Receipt;
        public bool IsWriteOff => Type == StockDocumentType.WriteOff;
        public bool IsInventory => Type == StockDocumentType.Inventory;
        public bool IsEditable => Status == StockDocumentStatus.Draft;
        public bool IsReadOnly => !IsEditable;
        public bool CanCancelDocument => Status == StockDocumentStatus.Posted && _currentUser.IsOwner;
        public bool CanDeleteDraft => Status == StockDocumentStatus.Draft && DocumentId is not null;
        public bool HasLines => Lines.Count > 0;
        public bool ShowEmptyHint => Lines.Count == 0 && IsEditable;

        public string AddButtonText => Type == StockDocumentType.Inventory ? "+ Mahsulotni sanash" : "+ Mahsulot qo'shish";

        public string Hint => Type switch
        {
            StockDocumentType.Receipt =>
                "Yetkazib beruvchidan kelgan tovarlarni qo'shing. Har bir mahsulot uchun miqdor va kirim narxi kiritiladi, sotuv narxi ustama bo'yicha taklif qilinadi.",
            StockDocumentType.WriteOff =>
                "Buzilgan, muddati o'tgan yoki yo'qolgan tovarlarni qo'shing. Izohda sababini yozing.",
            _ =>
                "Har bir mahsulotning haqiqatda sanalgan miqdorini kiriting. Tizimdagi qoldiq bilan farq o'tkazilganda tuzatiladi."
        };

        public string TotalLabel => Type switch
        {
            StockDocumentType.Receipt => "Jami (kirim narxida):",
            StockDocumentType.WriteOff => "Jami (tannarxda):",
            _ => "Farq (tannarxda):"
        };

        public string TotalText => NumberInput.Money(Lines.Sum(x => x.LineTotal)) + " so'm";
        public string LineCountText => $"{Lines.Count} ta mahsulot";

        // ---------------- Ochish ----------------

        public async Task InitializeNewAsync(StockDocumentType documentType, IEnumerable<(ProductDto Product, decimal Quantity)>? prefill = null)
        {
            Type = documentType;
            Status = StockDocumentStatus.Draft;
            InfoText = Hint;
            await LoadReferenceDataAsync();

            if (prefill is not null)
                foreach (var (product, quantity) in prefill)
                    AddOrMergeLine(CreateLine(product, quantity));
        }

        public async Task LoadAsync(int id)
        {
            await LoadReferenceDataAsync();
            var dto = await _stock.GetAsync(id, CancellationToken.None);

            DocumentId = dto.Id;
            Type = dto.Type;
            Number = dto.Number;
            Status = dto.Status;
            Note = dto.Note ?? "";
            CancelReason = dto.CancelReason ?? "";
            SelectedSupplier = Suppliers.FirstOrDefault(x => x.Id == dto.SupplierId);

            // Nofaol yetkazib beruvchi ro'yxatda bo'lmasa ham nomi ko'rinsin
            if (dto.SupplierId is not null && SelectedSupplier is null)
            {
                var missing = new SupplierDto { Id = dto.SupplierId.Value, Name = dto.SupplierName ?? "?" };
                Suppliers.Add(missing);
                SelectedSupplier = missing;
            }

            InfoText = dto.Status switch
            {
                StockDocumentStatus.Posted => $"O'tkazilgan: {dto.PostedAt:dd.MM.yyyy HH:mm}",
                StockDocumentStatus.Cancelled => $"Bekor qilingan: {dto.CancelledAt:dd.MM.yyyy HH:mm}. Sabab: {dto.CancelReason}",
                _ => $"Qoralama, yaratilgan: {dto.CreatedAt:dd.MM.yyyy HH:mm}. Qoldiqqa hali ta'sir qilmagan."
            };

            Lines.Clear();
            foreach (var line in dto.Lines)
            {
                // Qoralamada ustamani qayta hisoblash uchun mahsulot ma'lumotlari kerak
                ProductDto? product = null;
                if (dto.Status == StockDocumentStatus.Draft)
                {
                    try { product = await _products.GetAsync(line.ProductId, CancellationToken.None); }
                    catch (Exception) { /* mahsulot o'chirilgan bo'lishi mumkin */ }
                }

                Lines.Add(new StockLineViewModel(
                    dto.Type, _rules,
                    line.ProductId, line.ProductName, line.Unit,
                    line.CurrentSalePrice,
                    product?.MarkupPercent, product?.CategoryId,
                    line.Quantity, line.CostPrice, line.NewSalePrice,
                    line.ExpectedQuantity)
                {
                    CurrentStock = product?.Quantity,
                });
            }
        }

        private async Task LoadReferenceDataAsync()
        {
            _rules = await _pricing.GetRulesAsync(CancellationToken.None);

            Suppliers.Clear();
            if (_currentUser.CanManageStock)
                foreach (var supplier in await _suppliers.GetListAsync(includeInactive: false, CancellationToken.None))
                    Suppliers.Add(supplier);
        }

        // ---------------- Mahsulot oynasi uchun yordamchilar ----------------

        public Task<List<ProductDto>> SearchProductsAsync(string text, CancellationToken cancellation)
            => _products.SearchAsync(text, cancellation, take: 300);

        public Task<BarcodeLookupResult?> ResolveBarcodeAsync(string code)
            => _products.ResolveBarcodeAsync(code, CancellationToken.None);

        // Yangi qator: miqdor, narx va tavsiya etilgan sotuv narxi bilan
        public StockLineViewModel CreateLine(ProductDto product, decimal? quantity)
            => new(
                Type, _rules,
                product.Id, product.Name, product.Unit,
                product.Price, product.MarkupPercent, product.CategoryId,
                IsInventory ? quantity : quantity ?? 1,
                product.CostPrice,
                newSalePrice: null,
                expectedQuantity: IsInventory ? product.Quantity : null)
            {
                CurrentStock = product.Quantity,
            };

        public StockLineViewModel? FindLine(int productId) => Lines.FirstOrDefault(x => x.ProductId == productId);

        // Bir mahsulot hujjatda bir marta: bor bo'lsa almashtiriladi
        public void AddOrMergeLine(StockLineViewModel line)
        {
            ErrorMessage = "";
            var existing = FindLine(line.ProductId);
            if (existing is null)
            {
                Lines.Add(line);
                return;
            }

            Lines[Lines.IndexOf(existing)] = line;
        }

        public void ReplaceLine(StockLineViewModel oldLine, StockLineViewModel newLine)
        {
            var index = Lines.IndexOf(oldLine);
            if (index >= 0)
                Lines[index] = newLine;
            else
                AddOrMergeLine(newLine);
            OnPropertyChanged(nameof(TotalText));
        }

        // Skaner maydonida Enter: shtrix-kod bo'yicha topib, mahsulot oynasini ochadi
        [RelayCommand]
        private async Task ScanAsync()
        {
            var code = ScanText.Trim();
            if (code.Length == 0 || !IsEditable)
                return;

            try
            {
                var found = await _products.ResolveBarcodeAsync(code, CancellationToken.None);
                ScanText = "";
                if (found is null)
                {
                    ErrorMessage = $"Shtrix-kod bo'yicha mahsulot topilmadi: {code}";
                    return;
                }

                ErrorMessage = "";
                ProductScanned?.Invoke(found.Product, found.Quantity);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
        }

        [RelayCommand]
        private void RemoveLine(StockLineViewModel? line)
        {
            if (line is not null && IsEditable)
                Lines.Remove(line);
        }

        // ---------------- Saqlash / o'tkazish ----------------

        private bool CanEdit() => IsEditable && !IsBusy && _license.Current.IsActive;

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private Task SaveDraftAsync() => SaveAsync(post: false);

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private async Task PostAsync()
        {
            var message = Type switch
            {
                StockDocumentType.Receipt => "Kirimni o'tkazamizmi? Qoldiq va tannarx yangilanadi, yangi narxlar darhol amal qiladi.",
                StockDocumentType.WriteOff => "Tovarlarni hisobdan chiqaramizmi? Qoldiq kamayadi.",
                _ => "Inventarizatsiya natijasini o'tkazamizmi? Qoldiqlar sanalgan miqdorga tenglashtiriladi."
            };

            if (MessageBox.Show(message, "Hujjatni o'tkazish", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            await SaveAsync(post: true);
        }

        private async Task SaveAsync(bool post)
        {
            ErrorMessage = "";

            if (Lines.Count == 0)
            {
                ErrorMessage = "Kamida bitta mahsulot qo'shing.";
                return;
            }

            var invalid = Lines.Select(x => x.Validate()).FirstOrDefault(x => x is not null);
            if (invalid is not null)
            {
                ErrorMessage = invalid;
                return;
            }

            if (post && IsReceipt && Lines.Any(x => x.IsBelowCost))
            {
                var answer = MessageBox.Show(
                    "Ba'zi mahsulotlarning yangi sotuv narxi kirim narxidan past. Baribir o'tkazamizmi?",
                    "Tannarxdan arzon narx", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (answer != MessageBoxResult.Yes)
                    return;
            }

            var input = new StockDocumentInput
            {
                SupplierId = IsReceipt ? SelectedSupplier?.Id : null,
                Note = Note,
                Lines = Lines.Select(x => new StockLineInput
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity!.Value,
                    CostPrice = IsReceipt ? x.CostPrice!.Value : 0,
                    NewSalePrice = IsReceipt ? x.NewSalePrice : null,
                }).ToList(),
            };

            IsBusy = true;
            try
            {
                var saved = DocumentId is null
                    ? await _stock.CreateDraftAsync(Type, input, CancellationToken.None)
                    : await _stock.UpdateDraftAsync(DocumentId.Value, input, CancellationToken.None);

                DocumentId = saved.Id;
                Number = saved.Number;

                if (post)
                    await _stock.PostAsync(saved.Id, CancellationToken.None);

                await LoadAsync(saved.Id);
                DocumentChanged?.Invoke();
                InfoText = post ? $"Hujjat o'tkazildi. {InfoText}" : "Qoralama saqlandi. Qoldiqqa hali ta'sir qilmadi.";
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanDelete() => CanDeleteDraft && !IsBusy && _license.Current.IsActive;

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private async Task DeleteDraftAsync()
        {
            if (DocumentId is null)
                return;

            if (MessageBox.Show("Qoralamani o'chiramizmi?", "TijoratUz", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            IsBusy = true;
            try
            {
                await _stock.DeleteDraftAsync(DocumentId.Value, CancellationToken.None);
                DocumentChanged?.Invoke();
                CloseRequested?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanCancel() => CanCancelDocument && !IsBusy && _license.Current.IsActive;

        // Sabab oynadan so'raladi (code-behind), keyin shu buyruq chaqiriladi
        [RelayCommand(CanExecute = nameof(CanCancel))]
        private async Task CancelDocumentAsync(string? reason)
        {
            if (DocumentId is null)
                return;

            if (string.IsNullOrWhiteSpace(reason))
            {
                ErrorMessage = "Bekor qilish sababini kiriting.";
                return;
            }

            IsBusy = true;
            ErrorMessage = "";
            try
            {
                await _stock.CancelAsync(DocumentId.Value, reason, CancellationToken.None);
                await LoadAsync(DocumentId.Value);
                DocumentChanged?.Invoke();
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.InnerException?.Message ?? ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
