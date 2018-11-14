using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    /// <summary>
    /// Converter for bool properties from an enum value.
    /// Usage: 
    ///   BoolProperty="{Binding EnumProperty, Converter={StaticResource converter}, ConverterParameter={x:Static namespace:Enum.Value}}"
    /// </summary>
    internal class EnumBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(parameter) ?? DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.Equals(true) == true ? parameter : Binding.DoNothing;
        }
    }
}
