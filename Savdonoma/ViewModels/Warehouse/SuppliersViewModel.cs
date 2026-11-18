using Savdonoma.Converters;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Stock;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels.Warehouse
{
    // Yetkazib beruvchilar: ro'yxat, qo'shish/tahrirlash, qarz va to'lovlar
    public partial class SuppliersViewModel : ObservableObject
    {
        private readonly ISupplierService _service;

        public ObservableCollection<SupplierDto> Items { get; } = new();
        public ObservableCollection<SupplierPaymentDto> Payments { get; } = new();

        [ObservableProperty] private bool showInactive;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing), nameof(FormTitle), nameof(ToggleActiveText), nameof(DebtText))]
        private SupplierDto? selected;

        [ObservableProperty] private string name = "";
        [ObservableProperty] private string phone = "";
        [ObservableProperty] private string inn = "";
        [ObservableProperty] private string note = "";
        [ObservableProperty] private string paymentAmountText = "";
        [ObservableProperty] private string paymentNote = "";
        [ObservableProperty] private string message = "";
        [ObservableProperty] private bool hasError;
        [ObservableProperty] private string totalDebtText = "";

        public bool IsEditing => Selected is not null;
        public string FormTitle => Selected is null ? "Yangi yetkazib beruvchi" : "Yetkazib beruvchini tahrirlash";
        public string ToggleActiveText => Selected?.IsActive == false ? "Faollashtirish" : "Nofaol qilish";
        public string DebtText => Selected is null ? "" : FormatDebt(Selected.Debt);

        public SuppliersViewModel(ISupplierService service)
        {
            _service = service;
        }

        partial void OnShowInactiveChanged(bool value) => _ = LoadAsync();

        partial void OnSelectedChanged(SupplierDto? value)
        {
            Name = value?.Name ?? "";
            Phone = value?.Phone ?? "";
            Inn = value?.Inn ?? "";
            Note = value?.Note ?? "";
            PaymentAmountText = "";
            PaymentNote = "";
            _ = LoadPaymentsAsync();
        }

        public async Task LoadAsync()
        {
            try
            {
                var selectedId = Selected?.Id;
                var items = await _service.GetListAsync(ShowInactive, CancellationToken.None);

                Items.Clear();
                foreach (var item in items)
                    Items.Add(item);

                TotalDebtText = "Jami qarzimiz: " + NumberInput.Money(items.Where(x => x.Debt > 0).Sum(x => x.Debt)) + " so'm";
                Selected = selectedId is null ? null : Items.FirstOrDefault(x => x.Id == selectedId);
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        private async Task LoadPaymentsAsync()
        {
            Payments.Clear();
            if (Selected is null)
                return;

            try
            {
                foreach (var payment in await _service.GetPaymentsAsync(Selected.Id, CancellationToken.None))
                    Payments.Add(payment);
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        [RelayCommand]
        private void New()
        {
            Selected = null;
            Message = "";
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            try
            {
                var input = new SupplierInput { Name = Name, Phone = Phone, Inn = Inn, Note = Note };
                var saved = Selected is null
                    ? await _service.CreateAsync(input, CancellationToken.None)
                    : await _service.UpdateAsync(Selected.Id, input, CancellationToken.None);

                Selected = null;
                await LoadAsync();
                Selected = Items.FirstOrDefault(x => x.Id == saved.Id);
                Show("Saqlandi.");
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        [RelayCommand]
        private async Task ToggleActiveAsync()
        {
            if (Selected is null)
                return;

            try
            {
                var id = Selected.Id;
                var activate = !Selected.IsActive;
                await _service.SetActiveAsync(id, activate, CancellationToken.None);
                if (!activate && !ShowInactive)
                    Selected = null;
                await LoadAsync();
                Show(activate ? "Faollashtirildi." : "Nofaol qilindi. Unga yangi kirim qilib bo'lmaydi, tarixi saqlanadi.");
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        [RelayCommand]
        private async Task AddPaymentAsync()
        {
            if (Selected is null)
                return;

            if (!NumberInput.TryParseMoney(PaymentAmountText, out var amount) || amount <= 0)
            {
                Show("To'lov summasini butun son bilan kiriting.", error: true);
                return;
            }

            try
            {
                var id = Selected.Id;
                await _service.AddPaymentAsync(id, amount, PaymentNote, CancellationToken.None);
                await LoadAsync();
                Selected = Items.FirstOrDefault(x => x.Id == id);
                Show($"{NumberInput.Money(amount)} so'm to'lov yozildi.");
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        private void Show(string text, bool error = false)
        {
            Message = text;
            HasError = error;
        }

        public static string FormatDebt(long debt) => debt switch
        {
            > 0 => $"Qarzimiz: {NumberInput.Money(debt)} so'm",
            < 0 => $"Oldindan to'langan: {NumberInput.Money(-debt)} so'm",
            _ => "Qarz yo'q"
        };
    }
}
