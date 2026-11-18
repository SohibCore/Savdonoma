using Savdonoma.Core.Enums;
using Savdonoma.Core.Logic;
using Savdonoma.Converters;
using Savdonoma.Data.Services.Pricing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Savdonoma.ViewModels.Warehouse
{
    // Ombor hujjatidagi bitta qator (tahrirlanadigan)
    public partial class StockLineViewModel : ObservableObject
    {
        private readonly StockDocumentType _type;
        private readonly PricingRules? _pricing;
        private bool _priceEditedManually;
        private bool _settingSuggestedPrice;

        public int ProductId { get; }
        public string ProductName { get; }
        public Unit Unit { get; }
        public string UnitText => DisplayNames.Of(Unit);
        public long CurrentSalePrice { get; }
        public decimal? ProductMarkup { get; }
        public int? CategoryId { get; }

        // Inventarizatsiya: tizimdagi qoldiq. Hisobdan chiqarish: joriy tannarx shu yerda emas, CostPriceText'da.
        public decimal? ExpectedQuantity { get; }
        public string ExpectedText => ExpectedQuantity is null ? "" : NumberInput.Format(ExpectedQuantity.Value);

        // Mahsulot oynasida ko'rsatish uchun: hozirgi qoldiq
        public decimal? CurrentStock { get; init; }
        public string CurrentStockText => CurrentStock is null ? "—" : $"{NumberInput.Format(CurrentStock.Value)} {UnitText}";
        public string CurrentSalePriceText => NumberInput.Money(CurrentSalePrice) + " so'm";

        public bool IsReceipt => _type == StockDocumentType.Receipt;
        public bool IsWriteOff => _type == StockDocumentType.WriteOff;
        public bool IsInventory => _type == StockDocumentType.Inventory;

        // Jadvalda ko'rsatish uchun
        public string QuantityDisplay => Quantity is { } q ? $"{NumberInput.Format(q)} {UnitText}" : "—";
        public string CostPriceDisplay => CostPrice is { } c ? NumberInput.Money(c) : "—";
        public string NewSalePriceDisplay => NewSalePrice is > 0 ? NumberInput.Money(NewSalePrice.Value) : "o'zgarmaydi";

        // Sotuv narxi qaysi ustamadan hisoblanadi
        public string MarkupSourceText
        {
            get
            {
                if (_pricing is null)
                    return "";
                if (ProductMarkup is { } p)
                    return $"Mahsulot ustamasi {p:0.##}% bo'yicha hisoblandi";
                if (CategoryId is { } c && _pricing.CategoryMarkups.TryGetValue(c, out var cm))
                    return $"Kategoriya ustamasi {cm:0.##}% bo'yicha hisoblandi";
                if (_pricing.DefaultMarkupPercent is { } d)
                    return $"Umumiy ustama {d:0.##}% bo'yicha hisoblandi";
                return "Ustama belgilanmagan: sotuv narxini o'zingiz kiriting yoki bo'sh qoldiring (narx o'zgarmaydi)";
            }
        }

        // Tahrirlash oynasi uchun nusxa: "Bekor qilish" bosilsa asl qator o'zgarmaydi
        public StockLineViewModel Clone() => new(
            _type, _pricing, ProductId, ProductName, Unit, CurrentSalePrice, ProductMarkup, CategoryId,
            Quantity, CostPrice ?? 0, NewSalePrice is > 0 ? NewSalePrice : null, ExpectedQuantity)
        {
            CurrentStock = CurrentStock,
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal), nameof(LineTotalText), nameof(DifferenceText), nameof(DifferenceInfo), nameof(IsShortage), nameof(IsValid), nameof(QuantityDisplay))]
        private string quantityText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LineTotal), nameof(LineTotalText), nameof(MarkupText), nameof(IsBelowCost), nameof(IsValid), nameof(CostPriceDisplay))]
        private string costPriceText;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MarkupText), nameof(IsBelowCost), nameof(IsValid), nameof(PriceChangeText), nameof(NewSalePriceDisplay))]
        private string newSalePriceText;

        public StockLineViewModel(
            StockDocumentType type,
            PricingRules? pricing,
            int productId,
            string productName,
            Unit unit,
            long currentSalePrice,
            decimal? productMarkup,
            int? categoryId,
            decimal? quantity,
            long costPrice,
            long? newSalePrice,
            decimal? expectedQuantity)
        {
            _type = type;
            _pricing = pricing;
            ProductId = productId;
            ProductName = productName;
            Unit = unit;
            CurrentSalePrice = currentSalePrice;
            ProductMarkup = productMarkup;
            CategoryId = categoryId;
            ExpectedQuantity = expectedQuantity;

            quantityText = quantity is null ? "" : NumberInput.Format(quantity.Value);
            costPriceText = costPrice > 0 ? costPrice.ToString() : "";
            newSalePriceText = newSalePrice?.ToString() ?? "";

            // Saqlangan narx tavsiyadan farq qilsa, uni foydalanuvchi qo'lda kiritgan deb hisoblaymiz
            if (newSalePrice is not null && newSalePrice != SuggestedPrice)
                _priceEditedManually = true;
            else if (newSalePrice is null && type == StockDocumentType.Receipt)
                ApplySuggestion();
        }

        public decimal? Quantity => NumberInput.TryParseDecimal(QuantityText, out var q) ? q : null;
        public long? CostPrice => NumberInput.TryParseMoney(CostPriceText, out var c) ? c : null;
        public long? NewSalePrice => string.IsNullOrWhiteSpace(NewSalePriceText)
            ? null
            : NumberInput.TryParseMoney(NewSalePriceText, out var p) ? p : -1;

        public long? SuggestedPrice => _type == StockDocumentType.Receipt && CostPrice is { } cost
            ? _pricing?.SuggestSalePrice(cost, ProductMarkup, CategoryId)
            : null;

        // Kirim va hisobdan chiqarishda: miqdor x narx. Inventarizatsiyada: farq x tannarx.
        public long LineTotal
        {
            get
            {
                if (Quantity is not { } q || CostPrice is not { } c)
                    return 0;
                var amount = _type == StockDocumentType.Inventory ? q - (ExpectedQuantity ?? 0) : q;
                return SaleCalculator.RowSum(c, amount);
            }
        }

        public string LineTotalText => NumberInput.Money(LineTotal);

        public decimal? Difference => _type == StockDocumentType.Inventory && Quantity is { } q
            ? q - (ExpectedQuantity ?? 0)
            : null;

        public string DifferenceText => Difference switch
        {
            null => "",
            > 0 => "+" + NumberInput.Format(Difference.Value),
            _ => NumberInput.Format(Difference.Value)
        };

        public bool IsShortage => Difference is < 0;

        public string DifferenceInfo => Difference switch
        {
            null => "",
            0 => "Farq yo'q",
            < 0 => $"Kamomad: {NumberInput.Format(-Difference.Value)} {UnitText}",
            _ => $"Ortiqcha: {NumberInput.Format(Difference.Value)} {UnitText}"
        };

        public string MarkupText
        {
            get
            {
                if (NewSalePrice is not > 0 || CostPrice is not > 0)
                    return "";
                var markup = PriceCalculator.MarkupPercent(NewSalePrice.Value, CostPrice.Value);
                return markup is null ? "" : $"{markup:0.#}%";
            }
        }

        public bool IsBelowCost => NewSalePrice is > 0 && CostPrice is > 0 && NewSalePrice < CostPrice;

        public string PriceChangeText => NewSalePrice is > 0 && NewSalePrice != CurrentSalePrice
            ? $"{NumberInput.Money(CurrentSalePrice)} → {NumberInput.Money(NewSalePrice.Value)}"
            : "o'zgarmaydi";

        // Qator to'g'riligi: xato matni yoki null
        public string? Validate()
        {
            if (Quantity is not { } q)
                return $"'{ProductName}': miqdorni kiriting.";

            if (_type == StockDocumentType.Inventory ? q < 0 : q <= 0)
                return $"'{ProductName}': miqdor noto'g'ri.";

            if (_type == StockDocumentType.Receipt)
            {
                if (CostPrice is null)
                    return $"'{ProductName}': kirim narxini butun son bilan kiriting.";
                if (NewSalePrice is -1 or 0)
                    return $"'{ProductName}': yangi sotuv narxi noto'g'ri.";
            }

            return null;
        }

        public bool IsValid => Validate() is null;

        partial void OnCostPriceTextChanged(string value)
        {
            // Kirim narxi o'zgarsa, sotuv narxi ustama bo'yicha qayta hisoblanadi (qo'lda o'zgartirilmagan bo'lsa)
            if (!_priceEditedManually)
                ApplySuggestion();
        }

        partial void OnNewSalePriceTextChanged(string value)
        {
            if (!_settingSuggestedPrice)
                _priceEditedManually = !string.IsNullOrWhiteSpace(value) && NewSalePrice != SuggestedPrice;
        }

        private void ApplySuggestion()
        {
            _settingSuggestedPrice = true;
            NewSalePriceText = SuggestedPrice?.ToString() ?? "";
            _settingSuggestedPrice = false;
        }

        // "Ustama bo'yicha" tugmasi: qo'lda kiritilgan narxni bekor qilib tavsiyaga qaytaradi
        public void ResetToSuggestion()
        {
            _priceEditedManually = false;
            ApplySuggestion();
        }
    }
}
