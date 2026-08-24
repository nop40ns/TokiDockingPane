using System.Globalization;
using System.Windows;
using System.Windows.Data;
using TokiDockingPane.Interfaces;

namespace TokiDockingPane.Converters;

public class ZeroToVisibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EnumToolPainPosition v)
        {
            if (v != EnumToolPainPosition.None)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }

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
