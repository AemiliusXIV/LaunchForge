using System.Globalization;
using System.Windows.Data;

namespace LaunchForge.Converters;

// True → 1.0, False → 0.4 (for disabled steps in the list)
[ValueConversion(typeof(bool), typeof(double))]
public class BoolToOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? 1.0 : 0.4;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
