using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Roslyn.SyntaxVisualizer.Control
{
    public class HueToColorConverter : IValueConverter
    {
        public object Convert(object obj, Type targetType, object parameter, CultureInfo culture)
        {
            var hue = (double)obj;
            var saturation = 1.0d;
            var value = 1.0d;

            return ColorHelpers.HSVToColor(hue, saturation, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var color = (Color)value;
            return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B).GetHue();
        }


    }
}
