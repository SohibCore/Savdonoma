using Savdonoma.Core.Logic;
using Savdonoma.Converters;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using Savdonoma.Data.Services.Pricing;
using Savdonoma.Data.Services.Categories;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels.Warehouse
{
    // Narx sozlamalari (faqat rahbar): umumiy ustama, yaxlitlash, kategoriya ustamalari
    public partial class PricingSettingsViewModel : ObservableObject
    {
        private readonly IPricingService _pricing;
        private readonly ICategoryService _categories;

        public IReadOnlyList<int> RoundingSteps { get; } = PriceCalculator.AllowedRoundingSteps;
        public ObservableCollection<CategoryMarkupRow> Categories { get; } = new();

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExampleText))]
        private string defaultMarkupText = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ExampleText))]
        private int roundingStep = PriceCalculator.DefaultRoundingStep;

        [ObservableProperty] private string message = "";
        [ObservableProperty] private bool hasError;

        public PricingSettingsViewModel(IPricingService pricing, ICategoryService categories)
        {
            _pricing = pricing;
            _categories = categories;
        }

        // Jonli misol: 10 000 so'mlik tovar qanchaga sotiladi
        public string ExampleText
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DefaultMarkupText))
                    return "Umumiy ustama yo'q: kategoriya yoki mahsulotda ustama bo'lmasa, kirimda narx o'zgarmaydi.";
                if (!NumberInput.TryParseDecimal(DefaultMarkupText, out var markup))
                    return "";
                var price = PriceCalculator.SuggestSalePrice(10_000, markup, RoundingStep);
                return $"Misol: 10 000 so'mlik tovar → {NumberInput.Money(price ?? 0)} so'm";
            }
        }

        public async Task LoadAsync()
        {
            try
            {
                var rules = await _pricing.GetRulesAsync(CancellationToken.None);
                DefaultMarkupText = rules.DefaultMarkupPercent is null ? "" : NumberInput.Format(rules.DefaultMarkupPercent.Value);
                RoundingStep = rules.RoundingStep;

                Categories.Clear();
                foreach (var category in await _categories.GetListAsync(CancellationToken.None))
                    Categories.Add(new CategoryMarkupRow(category.Id, category.Name,
                        category.MarkupPercent is null ? "" : NumberInput.Format(category.MarkupPercent.Value)));

                Message = "";
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (!TryParseMarkup(DefaultMarkupText, out var defaultMarkup))
            {
                Show("Umumiy ustamani foizda kiriting (masalan, 20) yoki bo'sh qoldiring.", error: true);
                return;
            }

            var categoryMarkups = new List<(CategoryMarkupRow Row, decimal? Markup)>();
            foreach (var row in Categories)
            {
                if (!TryParseMarkup(row.MarkupText, out var markup))
                {
                    Show($"'{row.Name}' kategoriyasi ustamasi noto'g'ri.", error: true);
                    return;
                }
                categoryMarkups.Add((row, markup));
            }

            try
            {
                await _pricing.SaveDefaultsAsync(defaultMarkup, RoundingStep, CancellationToken.None);
                foreach (var (row, markup) in categoryMarkups)
                    await _pricing.SetCategoryMarkupAsync(row.Id, markup, CancellationToken.None);

                await LoadAsync();
                Show("Narx sozlamalari saqlandi. Ular keyingi kirimlardan boshlab qo'llanadi.");
            }
            catch (Exception ex)
            {
                Show(ex.InnerException?.Message ?? ex.Message, error: true);
            }
        }

        private static bool TryParseMarkup(string? text, out decimal? markup)
        {
            markup = null;
            if (string.IsNullOrWhiteSpace(text))
                return true;
            if (!NumberInput.TryParseDecimal(text.Replace("%", ""), out var value))
                return false;
            markup = value;
            return true;
        }

        private void Show(string text, bool error = false)
        {
            Message = text;
            HasError = error;
        }
    }

    public partial class CategoryMarkupRow : ObservableObject
    {
        public int Id { get; }
        public string Name { get; }
        [ObservableProperty] private string markupText;

        public CategoryMarkupRow(int id, string name, string markupText)
        {
            Id = id;
            Name = name;
            this.markupText = markupText;
        }
    }
}
