using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TokiDockingPane.Converters;

public class TrueToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            // TrueならCollapsed（非表示）、FalseならVisible（表示）
            return boolValue ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is Visibility visibility)
        {
            return visibility == Visibility.Collapsed;
        }
        return false;
    }
}
