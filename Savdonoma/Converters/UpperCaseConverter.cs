using System.Globalization;
using System.Windows.Data;

namespace Savdonoma.Converters
{
    /// <summary>Matnni katta harflarga o'giradi (jadval sarlavhalari uchun).</summary>
    public class UpperCaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is string s ? s.ToUpper(culture) : value ?? "";

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
