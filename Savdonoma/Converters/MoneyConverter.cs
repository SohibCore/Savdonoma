using System.Globalization;
using System.Windows.Data;

namespace Savdonoma.Converters
{
    public class MoneyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                long l => Format(l),
                int i => Format(i),
                _ => value ?? ""
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();

        // 35750 -> "35 750"
        private static string Format(long v)
            => v.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '\u00A0');
    }
}