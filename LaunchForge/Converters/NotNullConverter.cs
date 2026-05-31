using System.Globalization;
using System.Windows.Data;

namespace LaunchForge.Converters;

// Returns true when the value is not null; used to enable buttons
[ValueConversion(typeof(object), typeof(bool))]
public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object parameter, CultureInfo culture) =>
        value is not null;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
