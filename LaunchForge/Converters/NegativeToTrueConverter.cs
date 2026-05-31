using System.Globalization;
using System.Windows.Data;

namespace LaunchForge.Converters;

// Converts negative int to true; used for ProgressBar.IsIndeterminate before first step starts
[ValueConversion(typeof(int), typeof(bool))]
public class NegativeToTrueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is int i && i < 0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
