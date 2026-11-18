using System.Globalization;
using System.Windows.Data;
using Savdonoma.Core.Enums;

namespace Savdonoma.Converters
{
    // Enum qiymatlarining o'zbekcha nomlari
    public static class DisplayNames
    {
        public static string Of(StockDocumentType type) => type switch
        {
            StockDocumentType.Receipt => "Kirim",
            StockDocumentType.WriteOff => "Hisobdan chiqarish",
            StockDocumentType.Inventory => "Inventarizatsiya",
            _ => type.ToString()
        };

        public static string Of(StockDocumentStatus status) => status switch
        {
            StockDocumentStatus.Draft => "Qoralama",
            StockDocumentStatus.Posted => "O'tkazilgan",
            StockDocumentStatus.Cancelled => "Bekor qilingan",
            _ => status.ToString()
        };

        public static string Of(StockMovementType type) => type switch
        {
            StockMovementType.OpeningBalance => "Boshlang'ich qoldiq",
            StockMovementType.Receipt => "Kirim",
            StockMovementType.Sale => "Sotuv",
            StockMovementType.SaleCancel => "Sotuv bekor qilindi",
            StockMovementType.WriteOff => "Hisobdan chiqarish",
            StockMovementType.InventoryAdjustment => "Inventarizatsiya",
            StockMovementType.ManualAdjustment => "Qo'lda tuzatish",
            StockMovementType.DocumentCancel => "Hujjat bekor qilindi",
            _ => type.ToString()
        };

        public static string Of(Unit unit) => unit switch
        {
            Unit.Dona => "dona",
            Unit.Kg => "kg",
            Unit.Litr => "litr",
            Unit.Metr => "metr",
            Unit.Quti => "quti",
            _ => unit.ToString()
        };
    }

    public class DisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            StockDocumentType t => DisplayNames.Of(t),
            StockDocumentStatus s => DisplayNames.Of(s),
            StockMovementType m => DisplayNames.Of(m),
            Unit u => DisplayNames.Of(u),
            _ => value?.ToString() ?? ""
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Miqdor: 12.500 -> "12,5" (keraksiz nollarsiz)
    public class QuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
        {
            decimal d => d.ToString("0.###", CultureInfo.CurrentCulture),
            _ => value?.ToString() ?? ""
        };

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    // Matnli raqamlarni o'qish: "12 500", "12500", "1,5", "1.5"
    public static class NumberInput
    {
        public static bool TryParseDecimal(string? text, out decimal value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var clean = text.Replace(" ", "").Replace(" ", "").Replace(",", ".");
            return decimal.TryParse(clean, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
        }

        public static bool TryParseMoney(string? text, out long value)
        {
            value = 0;
            if (!TryParseDecimal(text, out var d) || d < 0 || d != Math.Floor(d))
                return false;
            value = (long)d;
            return true;
        }

        public static string Format(decimal value) => value.ToString("0.###", CultureInfo.CurrentCulture);
        public static string Money(long value) => value.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }
}
