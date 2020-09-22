using System;
using System.Globalization;
using System.Windows.Data;

namespace Roslyn.SyntaxVisualizer.Control
{
    public class PercentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (int)((double)value * 100);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => (int)value / 100.0d;
    }
}
